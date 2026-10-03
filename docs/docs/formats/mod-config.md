---
id: mod-config
title: Mod config
sidebar_label: Mod config
description: The key=value config.txt format read by mods at runtime.
---

# Mod config (`config.txt`)

Each mod ships a flat `key=value` text file that the mod reads **at runtime, inside the game process**. It is not read by the CLI; `inject` only copies it into place.

## Example

```ini
# Oink configuration
# Edit values then repack the mod.
Enabled=true
PigSkin=true
SpeedMultiplier=1.35
ToggleKey=O
PigTexture=npc/piggy1
```

## Syntax

| Rule | Detail |
| --- | --- |
| Encoding | read with `File.ReadAllLines` (UTF-8 with BOM detection) |
| Comments | a line whose first non-space character is `#` or `;` |
| Blank lines | ignored |
| Separator | the **first** `=` splits key from value; later `=` stay in the value |
| Whitespace | key and value are trimmed |
| Key matching | case-insensitive (`key.ToLowerInvariant()` in a `switch`) |
| Unknown keys | silently ignored |
| Missing keys | keep the built-in default |
| Parse failure | the whole load is wrapped in try/catch and falls back to defaults |

Values are stored as **strings** and converted at the point of use with `bool.TryParse` / `float.TryParse(…, CultureInfo.InvariantCulture)` plus a fallback, so `SpeedMultiplier=banana` degrades to `1.35` rather than crashing the render loop.

## Where it is loaded from

Both example mods probe, in order:

1. `<exe dir>\Content\<ModName>\config.txt` — preferred
2. `<exe dir>\<modname>.txt` — fallback (`oink.txt`, `quackmenu.txt`)

```csharp
string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "Oink", "config.txt");
if (!File.Exists(path))
{
    path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "oink.txt");
}
```

[`meatymod inject`](../cli/inject.md) writes **both** locations: it walks five directories up from the mod DLL to find the mod's `config.txt` and copies it to `<exe dir>\<Mod>.txt` and `<exe dir>\Content\<Mod>\config.txt`.

:::tip Editing without rebuilding
Because the file is read at startup from the game folder, you can tweak values and relaunch — no rebuild, no re-inject. Edit `game\Blood and Bacon\Content\<Mod>\config.txt`. Remember that `inject` overwrites it from the repository copy on the next run.
:::

## Shipped keys

### `mods\Oink\config.txt`

| Key | Default | Type | Meaning |
| --- | --- | --- | --- |
| `Enabled` | `true` | bool | master switch applied at inject time |
| `PigSkin` | `true` | bool | swap the player skin texture |
| `SpeedMultiplier` | `1.35` | float | multiplies the player's `sprint` field each frame; `<= 0` or exactly `1` disables the effect |
| `ToggleKey` | `O` | `Keys` enum name | in-game toggle, parsed case-insensitively |
| `PigTexture` | `npc/piggy1` | content path | passed to `Content.Load` |

### `mods\QuackMenu\config.txt`

| Key | Default | Type | Meaning |
| --- | --- | --- | --- |
| `CreativeMode` | `true` | bool | sets the cheat flags on load |
| `FlatWorld` | `true` | bool | forces day 1 and a fixed spawn height |
| `SpawnHeight` | `3` | float | value written to the screen manager's `spawnY` |
| `OpenMenuKey` | `F1` | `Keys` enum name | opens the boss menu |
| `Bosses` | `Cutty,Princess,BoarKing,Twin` | comma list | filters the boss catalog by name |

Key names in the file do not always match the C# field: `CreativeMode` maps to `QuackConfig.CreativeModeEnabled`.

## Key names

`ToggleKey` / `OpenMenuKey` are parsed with `Enum.Parse(typeof(Keys), value, ignoreCase: true)` over XNA's `Microsoft.Xna.Framework.Input.Keys`, so `O`, `o`, `F1`, `NumPad5`, `OemTilde` are all valid. An unrecognised name falls back to the mod's default.

## Adding config to your own mod

```csharp
public class MyConfig
{
    public string Enabled = "true";
    public string Intensity = "1.0";

    public static MyConfig Load()
    {
        var cfg = new MyConfig();
        try
        {
            var dir  = AppDomain.CurrentDomain.BaseDirectory;
            var path = Path.Combine(dir, "Content", "MyMod", "config.txt");
            if (!File.Exists(path)) { path = Path.Combine(dir, "mymod.txt"); }
            if (!File.Exists(path)) { return cfg; }

            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";")) { continue; }

                var eq = line.IndexOf('=');
                if (eq < 0) { continue; }

                var key   = line.Substring(0, eq).Trim().ToLowerInvariant();
                var value = line.Substring(eq + 1).Trim();

                switch (key)
                {
                    case "enabled":   cfg.Enabled = value;   break;
                    case "intensity": cfg.Intensity = value; break;
                }
            }
        }
        catch { /* defaults */ }

        return cfg;
    }
}
```

Put `config.txt` at your mod's root (next to `manifest.json`) so `inject`'s five-levels-up probe finds it. See [Authoring a mod](../mods/authoring-a-mod.md).
