---
id: overview
title: Mods
sidebar_label: Overview
description: The two example mods shipped with MeatyMod and the shape they share.
---

# Mods

Two example mods ship with the repository. They are **standalone** — not part of the suite's solution — but they are packed, injected and launched through it.

| Mod | What it does | Toggle | Entry type |
| --- | --- | --- | --- |
| [QuackMenu](./quackmenu.md) | creative mode flags, flat world, boss spawner menu | `F1` | `QuackMenu.QuackMenuEntry` |
| [Oink](./oink.md) | pig skin texture + sprint multiplier | `O` | `Oink.OinkEntry` |

Both target **.NET Framework 4.0** with `LangVersion 7.3` and reference XNA 4.0 assemblies vendored under their own `lib\xna\` — the game is an XNA 4.0 title, so a mod must bind to the same framework the game loads.

## Shared directory shape

```text
mods/<Name>/
├─ manifest.json            id, name, version, author, description
├─ config.txt               key=value runtime settings
├─ build.bat                dotnet build -c Release
├─ run.bat                  build → restore → inject → launch → restore
├─ README.md
├─ .gitignore
├─ lib/xna/*.dll            XNA 4.0 reference assemblies
└─ src/<Name>/
   ├─ <Name>.csproj
   ├─ <Name>Entry.cs        static Inject(Game) — the injector's target
   ├─ <Name>Hook.cs         GameComponent that drives Update()
   └─ …feature files
```

`inject` relies on that exact depth: it looks for `config.txt` five directories above the built DLL (`bin\<cfg>\<tfm>` → `src\<Name>` → mod root).

## Shared runtime pattern

1. `Inject(Game)` — cheap setup only: load config, add the hook component, set `_injected`.
2. A `GameComponent` hook gets a per-frame `Update` callback.
3. Each frame, find `Blood.ScreenManager` in `Game.Components` (retrying until it appears).
4. Reach into the internal game state through reflection, failing soft and logging anything unexpected.

Full detail in [Mod runtime model](../architecture/mod-runtime.md).

## Running them

```bat
:: one command: build, restore, inject, launch, restore again
mods\Oink\run.bat
mods\QuackMenu\run.bat

:: assume the DLLs are already built
mods\launch\launch-oink.bat
mods\launch\launch-quackmenu.bat
mods\launch\launch-both.bat
```

The `launch-*` scripts error out with the build command if a DLL is missing; the `run.bat` scripts build first. Both restore the executable before injecting and again after the game exits, so the game directory is never left patched.

## Running both at once

```bat
meatymod inject "game\Blood and Bacon\BloodandBacon.exe" ^
  --mod mods\Oink\src\Oink\bin\Release\net40\Oink.dll ^
  --mod mods\QuackMenu\src\QuackMenu\bin\Release\net40\QuackMenu.dll
```

Each mod gets its own `call` in the constructor, its own hook component and its own log file. They do not know about each other — the only coupling is `UpdateOrder` (`int.MinValue` for QuackMenu's hook, `int.MaxValue` for Oink's).

## Logs

| Mod | Log file |
| --- | --- |
| Oink | `<game dir>\oink.log` |
| QuackMenu | `<game dir>\quackmenu.log` |

Appended with a `yyyy-MM-dd HH:mm:ss` prefix, and never allowed to throw. An empty or missing log after launching means `Inject` never ran — see [Troubleshooting](../reference/troubleshooting.md).

## Writing your own

Start at [Authoring a mod](./authoring-a-mod.md). The only hard requirement is a type with `public static void Inject(Microsoft.Xna.Framework.Game game)`.
