# QuackMenu

Creative mode mod for **Blood & Bacon** (XNA 4.0, Steam app 434570).

Standalone mod. Not part of MeatyMod suite. Packed and installed through the suite CLI.

## Features

- **Creative mode** — spawn flags enabled on load (`hostAllowCheats`, `myplayerCheats`, `allWeapons`, `developer`).
- **Flat world** — forces day 1 and a flat spawn height so you start on open ground.
- **Boss spawner menu** — press **F1** to open a menu that spawns bosses:

  | Boss | Game class | Model prefix |
  |------|-----------|--------------|
  | Cutty | `Blood.Cutty4` | `cutty_` |
  | Princess | `Blood.Princess4` | `princess_` |
  | BoarKing | `Blood.boarDupe6` | `boar` |
  | Twin | `Blood.Twin` | `twin_` |

  Navigate with **Up/Down**, **Enter** to spawn, **Esc** to close.

## Build

Requires XNA 4.0 runtime (GAC) — XNA reference assemblies are bundled under `lib/xna/`.

```
dotnet build src\QuackMenu\QuackMenu.csproj
```

Output: `src\QuackMenu\bin\Debug\net40\QuackMenu.dll`

## Pack (via MeatyMod suite)

From repo root:

```
meaty pack mods\QuackMenu
```

This zips the mod folder into `mod.zip` per `manifest.json`.

## Install

The mod must be loaded inside the game process. Two paths:

1. **Assembly injection** — use `MeatyMod.Injector` to patch `Blood.myGame` ctor so it calls `QuackMenu.QuackMenuEntry.Inject(this)` after `ScreenManager` creation. The suite injector is a skeleton; the patch call site is the documented integration point.
2. **Manual** — place `QuackMenu.dll` + XNA deps beside `BloodandBacon.exe` and add the `Inject` call via any XNA4 assembly patcher.

On load the mod writes `quackmenu.log` next to the game exe for diagnostics.

## Config

`config.txt` — edit and repack to change behavior:

```
CreativeMode=true
FlatWorld=true
SpawnHeight=3
OpenMenuKey=F1
Bosses=Cutty,Princess,BoarKing,Twin
```

## Notes / Limits

- Boss classes are `internal` in the game assembly; spawning uses reflection. Constructor signatures vary — unsupported signatures are logged and skipped rather than crashing.
- Flat world forces `curDay = 1`; terrain comes from the game's own facility generator.
- Read-only research in `.ai/report/`; unknown game internals are logged, never assumed.
