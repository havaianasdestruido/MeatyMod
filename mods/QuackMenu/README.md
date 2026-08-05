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

The mod must be loaded inside the game process via IL injection into `BloodandBacon.exe`. One command does it all:

```
dotnet build src\MeatyMod.Cli\MeatyMod.Cli.csproj
meatymod inject "game\Blood and Bacon\BloodandBacon.exe" "mods\QuackMenu\src\QuackMenu\bin\Debug\net40\QuackMenu.dll"
```

What `inject` does:

1. Backs up the game exe to `BloodandBacon.exe.backup`.
2. Patches `Blood.myGame` ctor to call `QuackMenuEntry.Inject(this)` (Mono.Cecil IL patch).
3. Copies `QuackMenu.dll` + `config.txt` next to the game exe.

Then launch the game normally (via Steam). In-game press **F1** for the boss spawner menu.

**Restore the original exe:**

```
meatymod restore "game\Blood and Bacon\BloodandBacon.exe"
```

## Manual injection

Place `QuackMenu.dll` + XNA deps beside `BloodandBacon.exe` and add the `Inject` call via any XNA4 assembly patcher.

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
