---
id: installation
title: Installation
sidebar_label: Installation
description: Prerequisites, repository checkout and building the MeatyMod CLI and example mods.
---

# Installation

## Prerequisites

| Requirement | Why | Notes |
| --- | --- | --- |
| Windows | the game, the injector output and the QA tools are Windows-only | the libraries themselves are portable, but `modharness`/`memscan` use Windows APIs |
| [.NET SDK 10](https://dotnet.microsoft.com/download) | every suite project targets `net10.0` | set in `Directory.Build.props` |
| .NET Framework 4.0 targeting pack | the example mods target `net40` | installed with Visual Studio or the [developer pack](https://dotnet.microsoft.com/download/dotnet-framework/net40) |
| XNA 4.0 redistributable | the game and the mods bind to XNA at runtime | reference assemblies are vendored per-mod under `lib\xna\` |
| Blood & Bacon | the target of every inject | Steam app 434570 |
| PowerShell 5+ | `tools\release.ps1`, `tools\smoke.ps1` | ships with Windows |

:::note The `game\` folder

Scripts resolve the game at `game\Blood and Bacon\BloodandBacon.exe`, relative to the repository root. Copy (or junction) your installed game there. The folder is git-ignored and must be treated as read-only input — the only file MeatyMod ever rewrites inside it is the executable you explicitly point `inject` at.

```bat
mklink /J "game\Blood and Bacon" "C:\Program Files (x86)\Steam\steamapps\common\Blood and Bacon"
```

:::

## Clone

```bat
git clone https://github.com/havaianasdestruido/MeatyMod.git
cd MeatyMod
```

## Build everything

`all.bat` is the one-shot build: the `src` solution, both QA tools and both example mods, all in Release.

```bat
all.bat
```

It stops at the first failure and prints which stage failed:

```text
=== [all] Building MeatyMod solution (src) ===
=== [all] Building tools\modharness ===
=== [all] Building tools\memscan ===
=== [all] Building mods\Oink ===
=== [all] Building mods\QuackMenu ===
=== [all] All builds OK ===
```

## Build selectively

```bat
:: suite only
dotnet build src\MeatyMod.sln

:: the CLI only
dotnet build src\MeatyMod.Cli\MeatyMod.Cli.csproj

:: one mod
dotnet build mods\Oink\src\Oink\Oink.csproj -c Release
```

## Where the binaries land

| Artifact | Path |
| --- | --- |
| `meatymod.exe` (Debug) | `src\MeatyMod.Cli\bin\Debug\net10.0\meatymod.exe` |
| `meatymod.exe` (Release) | `src\MeatyMod.Cli\bin\Release\net10.0\meatymod.exe` |
| `Oink.dll` | `mods\Oink\src\Oink\bin\Release\net40\Oink.dll` |
| `QuackMenu.dll` | `mods\QuackMenu\src\QuackMenu\bin\Release\net40\QuackMenu.dll` |
| `ModHarness.exe` | `tools\modharness\bin\Release\net10.0-windows\ModHarness.exe` |
| `memscan.exe` | `tools\memscan\bin\Release\net10.0\memscan.exe` |

The `run.bat` / `launch-*.bat` helpers pick whichever `meatymod.exe` is newer between the Debug and Release output, so you do not have to remember which configuration you last built.

## Put `meatymod` on PATH (optional)

Every example in these docs writes `meatymod …`. If you have not added the build output to `PATH`, use `dotnet run` instead:

```bat
dotnet run --project src\MeatyMod.Cli --no-build -- pack mods\Oink
```

…or add it for the current shell:

```bat
set "PATH=%CD%\src\MeatyMod.Cli\bin\Release\net10.0;%PATH%"
meatymod
```

Running `meatymod` with no arguments prints the command list and exits with code `1`.

## Verify the install

```bat
dotnet test src\MeatyMod.Tests\MeatyMod.Tests.csproj
meatymod verify "game\Blood and Bacon\Content"
```

See [Testing](../development/testing.md) for what the suite covers, and [Quickstart](./quickstart.md) for the first end-to-end run.
