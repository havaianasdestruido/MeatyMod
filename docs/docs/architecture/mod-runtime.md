---
id: mod-runtime
title: Mod runtime model
sidebar_label: Mod runtime
description: How an injected mod finds the game, hooks the update loop and talks to internal game types through reflection.
---

# Mod runtime model

Once the [injection pipeline](./injection-pipeline.md) has added the `Inject(this)` call, everything else is ordinary XNA code running inside the game's `AppDomain`. Both example mods follow the same four-stage pattern, and new mods should too.

## The contract

A mod DLL must expose one type with:

```csharp
public static void Inject(Microsoft.Xna.Framework.Game game)
```

That is the *entire* API surface the injector requires. Everything after it is your code.

## Stage 1 — `Inject`: register, do not act

`Inject` runs inside `Blood.myGame`'s constructor. At that moment:

- the `Game` object exists, but `Initialize()`, `LoadContent()` and the first `Update()` have **not** run;
- `game.Components` is typically empty — `Blood.ScreenManager` is not there yet;
- `game.Content` exists but the graphics device may not be ready, so `Content.Load<T>` is unsafe.

So `Inject` does the cheap, safe things only:

```csharp
public static void Inject(Game game)
{
    if (_injected) { return; }          // idempotent: survives a double patch

    _game = game;
    _config = OinkConfig.Load();        // plain file read
    FindScreenManager(game);            // may legitimately find nothing yet

    if (!game.Components.Contains(_hook))
    {
        _hook = new OinkHook(game);     // GameComponent
        game.Components.Add(_hook);
    }

    _injected = true;
    Log("Oink injected.");
}
```

## Stage 2 — the hook component

Adding a `GameComponent` is how a mod gets a per-frame callback without patching `Update`:

```csharp
public class OinkHook : GameComponent
{
    public OinkHook(Game game) : base(game)
    {
        UpdateOrder = int.MaxValue;     // run after the game has updated
    }

    public override void Update(GameTime gameTime)
    {
        OinkEntry.Update();
        base.Update(gameTime);
    }
}
```

`UpdateOrder` is a deliberate choice:

| Mod | `UpdateOrder` | Why |
| --- | --- | --- |
| Oink | `int.MaxValue` | overwrite `sprint` *after* the game recomputed it this frame |
| QuackMenu | `int.MinValue` | read input before the game consumes it |
| `BossMenuScreen` | `int.MaxValue - 1` (`Update` **and** `Draw`) | draw the overlay on top of the game |

## Stage 3 — find the screen manager

The real game state lives on `Blood.ScreenManager`, a component the game adds during its own startup. Mods look it up by full type name and keep retrying until it exists:

```csharp
foreach (var c in game.Components)
{
    if (c != null && c.GetType().FullName == "Blood.ScreenManager")
    {
        _screenManager = c;
        return;
    }
}
Log("ScreenManager not found in Components.");
```

From there, Oink walks the manager's private `screens` list to find the active gameplay screen:

```csharp
var field = screenManager.GetType()
    .GetField("screens", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

foreach (var elem in (IList)field.GetValue(screenManager))
{
    if (elem != null && elem.GetType().FullName.StartsWith("Blood.BloodnBacon"))
    {
        return elem;      // the gameplay screen
    }
}
```

Both lookups are re-run every frame while the result is `null`, and transitions are logged once (`Game screen found: …` / `Game screen lost (null).`) rather than every frame.

## Stage 4 — reflection against internal game types

Every interesting game type is `internal`, so mods never compile against the game assembly. They use `BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance` helpers that **fail soft**:

```csharp
public static object GetField(object target, string name) { /* try/catch → null */ }
public static void   SetField(object target, string name, object value) { /* try/catch → log */ }
```

The house rule, stated in both mod READMEs: *unknown game internals are logged, never assumed*. A missing field produces a log line and a no-op, not an exception inside the render loop.

Known field and type names used by the shipped mods:

| Symbol | Type | Used by |
| --- | --- | --- |
| `Blood.myGame` | class (`Game` subclass) | injector host |
| `Blood.ScreenManager` | component | both mods |
| `Blood.ScreenManager.screens` | `IList` | Oink |
| `Blood.BloodnBacon` | gameplay screen | both mods |
| `player1Texture`, `player1TextureOrig` | `Texture2D` fields | Oink skin swap |
| `myPlayer.sprint` | `float` field | Oink speed |
| `hostAllowCheats`, `myplayerCheats`, `allWeapons`, `developer` | `bool` fields | QuackMenu creative mode |
| `curDay`, `currentDay`, `tempcurrentDay`, `spawnY` | fields | QuackMenu flat world |
| `Blood.Cutty4`, `Blood.Princess4`, `Blood.boarDupe6`, `Blood.Twin` | boss classes | QuackMenu spawner |

## Configuration

Both mods read a flat `key=value` file, probing two locations in order:

1. `<exe dir>\Content\<ModName>\config.txt`
2. `<exe dir>\<modname>.txt`

`inject` writes both. Parsing rules: lines are trimmed; empty lines and lines starting with `#` or `;` are skipped; the first `=` splits key from value; keys are matched case-insensitively; unknown keys are ignored; the whole load is wrapped in a try/catch that falls back to defaults. See [mod config format](../formats/mod-config.md).

Values are stored as **strings** and parsed at use time with `bool.TryParse` / `float.TryParse(…, CultureInfo.InvariantCulture)` plus a fallback, so a malformed value degrades to the default instead of throwing.

## Input handling

Mods poll `Keyboard.GetState()` inside their hook and implement edge detection manually, because the game also reads the keyboard:

```csharp
Keys toggleKey = ParseKey(_config.ToggleKey, Keys.O);
bool now = IsKeyDown(toggleKey);
bool was = _lastKeys.TryGetValue(toggleKey, out bool w) && w;
_lastKeys[toggleKey] = now;

if (now && !was) { SetEnabled(!_enabled); }   // fires once per press
```

`ParseKey` uses `Enum.Parse(typeof(Keys), s, ignoreCase: true)` with a fallback, so `ToggleKey=O`, `ToggleKey=o` and `ToggleKey=F1` all work and garbage falls back to the default.

## Logging

There is no shared logging library; each mod appends to its own file next to the executable and swallows any failure:

```csharp
File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "oink.log"),
    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message + Environment.NewLine);
```

`oink.log` and `quackmenu.log` are the primary diagnostic surface — see [Troubleshooting](../reference/troubleshooting.md).

## Lifetime and state

All mod state is `static`, because the entry type is never instantiated and the game is a single process with a single `myGame`. The consequences:

- `_injected` makes `Inject` idempotent.
- There is no unload path. Disabling a mod at runtime means reverting its effects (Oink's `OinkSkin.Restore`), not unloading the assembly.
- Original values are stashed on first write (`_original`, `_stashed`) so a toggle-off can restore them.
