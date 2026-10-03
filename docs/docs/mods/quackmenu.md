---
id: quackmenu
title: QuackMenu
sidebar_label: QuackMenu
description: Creative mode, flat world and an in-game boss spawner menu for Blood & Bacon.
---

# QuackMenu

Creative-mode mod: cheat flags on load, a flat day-1 spawn, and an `F1` overlay menu that spawns bosses.

| | |
| --- | --- |
| Id / version | `quackmenu` / `1.0.0` |
| Entry type | `QuackMenu.QuackMenuEntry` |
| Target | `net40`, XNA 4.0 |
| Log | `quackmenu.log` |
| Toggle | `F1` |

## Features

### Creative mode

On inject, `CreativeMode.Apply` sets four boolean fields on `Blood.ScreenManager` by reflection:

```text
hostAllowCheats = true
myplayerCheats  = true
allWeapons      = true
developer       = true
```

### Flat world

When `FlatWorld=true`, it also forces the day counter and the spawn height:

```text
curDay = currentDay = tempcurrentDay = 1
spawnY = <SpawnHeight>        (default 3)
```

Terrain still comes from the game's own facility generator — "flat" here means *day 1, fixed spawn height*, not a replaced heightmap.

Every write goes through a try/catch helper that logs and continues, so a renamed field in a future game build degrades to a no-op.

### Boss spawner menu

Press `F1` to toggle `BossMenuScreen`, a `DrawableGameComponent` with `DrawOrder`/`UpdateOrder` of `int.MaxValue - 1`.

| Key | Action |
| --- | --- |
| `Up` / `Down` | move the selection (wraps) |
| `Enter` | spawn the selected boss |
| `Esc` | close the menu |
| `F1` | toggle the menu |

The overlay draws a 70 %-opaque black full-screen quad, a title, the boss list with a `>` marker on the selection (yellow) and a hint line. It tries `Content.Load<SpriteFont>("QuackMenu")`, falls back to `"Arial"`, and if neither exists logs `No usable sprite font found; menu text will not render.` — the menu still works, it is just invisible.

Input uses explicit edge detection against the previous `KeyboardState`, so holding a key does not repeat.

## Boss catalog

`BossCatalog.All`:

| Name | Game class | Model prefix | Weight |
| --- | --- | --- | --- |
| Cutty | `Blood.Cutty4` | `cutty_` | 0 |
| Princess | `Blood.Princess4` | `princess_` | 1 |
| BoarKing | `Blood.boarDupe6` | `boar` | 2 |
| Twin | `Blood.Twin` | `twin_` | 3 |

## How spawning works

`BossSpawner.Spawn` is pure reflection, because the boss classes are `internal`:

1. `Type.GetType(def.ClassName)` — not found ⇒ `Boss class not found: <class>` and return.
2. Enumerate **all** constructors (public and non-public) and, for the first one whose parameters can be satisfied, invoke it. `BuildArgs` maps parameters by type:

   | Parameter type | Value supplied |
   | --- | --- |
   | `int` | `def.Weight` |
   | `Blood.ScreenManager` | the live screen manager |
   | `string` | `def.ModelPrefix + <parameter index>` |
   | `bool` | `false` |
   | anything else | unsupported — this constructor is skipped |

3. No constructor satisfied ⇒ `Boss could not be constructed: <class>`.
4. `RegisterBoss` looks for a `bosses` field on the screen manager and, if it is an `IList`, adds the instance. Otherwise: `No boss list field found; instance left standalone.`

Every failure is logged, never thrown — an unsupported signature skips that boss rather than crashing the game.

## Configuration

`mods\QuackMenu\config.txt`:

```ini
# QuackMenu configuration
# Edit values then repack the mod.
CreativeMode=true
FlatWorld=true
SpawnHeight=3
OpenMenuKey=F1
Bosses=Cutty,Princess,BoarKing,Twin
```

| Key | Default | Effect |
| --- | --- | --- |
| `CreativeMode` | `true` | master switch for the cheat flags (maps to `QuackConfig.CreativeModeEnabled`) |
| `FlatWorld` | `true` | force day 1 + `spawnY` |
| `SpawnHeight` | `3` | the `spawnY` value |
| `OpenMenuKey` | `F1` | any `Microsoft.Xna.Framework.Input.Keys` name, case-insensitive |
| `Bosses` | `Cutty,Princess,BoarKing,Twin` | intended as a catalog filter; **parsed but unused** (below) |

:::caution Two config keys are currently inert
`BossCatalog.Enabled(config)` exists and correctly filters the catalog by the `Bosses` list, but `BossMenuScreen` initialises its list from `BossCatalog.All` — so the menu always shows all four. `BossWeights` is parsed into `QuackConfig.BossWeights` and never read; weights come from the hard-coded catalog. Both are good first contributions.
:::

## Build and run

```bat
mods\QuackMenu\build.bat
:: → mods\QuackMenu\src\QuackMenu\bin\Release\net40\QuackMenu.dll

mods\QuackMenu\run.bat
:: build → restore → inject → launch → restore
```

Manual:

```bat
meatymod inject "game\Blood and Bacon\BloodandBacon.exe" ^
  --mod mods\QuackMenu\src\QuackMenu\bin\Release\net40\QuackMenu.dll
meatymod restore "game\Blood and Bacon\BloodandBacon.exe"
```

## Source map

| File | Responsibility |
| --- | --- |
| `QuackMenuEntry.cs` | `Inject(Game)`, key handling, menu open/close, `SpawnBoss`, logging |
| `QuackMenuHook.cs` | `GameComponent` with `UpdateOrder = int.MinValue` |
| `BossMenuScreen.cs` | `DrawableGameComponent` overlay: input, layout, drawing |
| `BossCatalog.cs` | `BossDefinition` records and the `All` / `Enabled` lists |
| `BossSpawner.cs` | constructor matching, instantiation, registration |
| `CreativeMode.cs` | the cheat/flat-world field writes |
| `QuackConfig.cs` | `config.txt` parsing with defaults |

## Known limits

- Boss constructor signatures vary between game builds; unsupported shapes are logged and skipped.
- "Flat world" forces `curDay = 1`; it does not modify terrain.
- The menu needs a sprite font in the game's content to render text.
- `Bosses` / `BossWeights` config keys are not wired up (above).

## Related

- [Oink](./oink.md) · [Authoring a mod](./authoring-a-mod.md)
- [Mod runtime model](../architecture/mod-runtime.md)
