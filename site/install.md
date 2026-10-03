---
title: Install
eyebrow: Getting started
description: Build the suite from source, point it at your game, and launch a mod — in three minutes on a Windows machine.
---

MeatyMod is distributed as source. There is no installer: you build one executable,
point it at a Blood &amp; Bacon install, and run it.

## Before you start

| Requirement | Why |
| --- | --- |
| Windows 10 or 11 | The game, the injector output and `memscan` are Windows-only. |
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | Builds the suite, the tools and the test project. |
| .NET Framework 4.0 targeting pack | The mod DLLs target `net40`, because the game does. |
| Blood &amp; Bacon | Your own copy. No game files are distributed here. |

Check the SDK first:

```bat
dotnet --version
```

## 1. Clone and build

```bat
git clone https://github.com/havaianasdestruido/MeatyMod.git
cd MeatyMod
all.bat
```

`all.bat` builds, in order: the main solution, `ModHarness`, `memscan`, Oink and QuackMenu —
all in Release. It stops at the first failure.

The pieces you will use afterwards:

```text
src\MeatyMod.Cli\bin\Release\net10.0\meatymod.exe
mods\Oink\src\Oink\bin\Release\net40\Oink.dll
mods\QuackMenu\src\QuackMenu\bin\Release\net40\QuackMenu.dll
```

Put the CLI on your `PATH`, or call it by full path — the examples below assume it is on `PATH`.

## 2. Point MeatyMod at the game

Every script in the repository expects the install at `game\Blood and Bacon\`. Either copy
the game folder there, or link it:

```bat
mklink /J "game\Blood and Bacon" ^
  "C:\Program Files (x86)\Steam\steamapps\common\Blood and Bacon"
```

`game\` is git-ignored, so nothing from your install is ever committed.

> **A junction is the live install.** Injecting through it patches your real game files.
> That is supported and reversible — but if you would rather experiment on a throwaway,
> copy the folder instead of linking it.

Confirm the content is readable before you change anything:

```bat
meatymod verify "game\Blood and Bacon\Content"
```

A healthy stock install reports `Valid: 1860  Invalid: 0`.

## 3. Launch a mod

The bundled launchers restore the executable, inject, and start the game:

```bat
mods\launch\launch-quackmenu.bat
mods\launch\launch-oink.bat
mods\launch\launch-both.bat
```

Or do it by hand:

```bat
meatymod inject "game\Blood and Bacon\BloodandBacon.exe" ^
  --mod mods\QuackMenu\src\QuackMenu\bin\Release\net40\QuackMenu.dll
```

In game, press <kbd>F1</kbd> for the QuackMenu overlay or <kbd>O</kbd> for Oink.

## 4. Undo it

```bat
meatymod restore "game\Blood and Bacon\BloodandBacon.exe"
```

That copies `BloodandBacon.exe.backup` back over the patched file. Content installs are
undone by copying `game\Blood and Bacon\Backups\MeatyMod\` back over `Content\`. As a last
resort, Steam's *Verify integrity of game files* rebuilds everything.

## Installing a content mod

Content mods are packaged archives rather than DLLs:

```bat
meatymod pack mods\MyTextures mytextures.zip
meatymod install mytextures.zip "game\Blood and Bacon"
```

When the archive contains a `checksums.txt`, `install` verifies every path it lists before
writing anything, and aborts on the first mismatch. An archive **without** that file installs
with no integrity verification at all, and files the list omits are extracted unchecked —
`pack` always writes a complete list, so this only affects hand-made archives. Entries that
would escape `Content\` are refused either way, and every replaced file is backed up.

## If something goes wrong

Start with the [troubleshooting guide]({{ site.docs_url | append: 'reference/troubleshooting' | relative_url }}) —
it lists every error message the tools emit, with causes and fixes. The
[installation page]({{ site.docs_url | append: 'getting-started/installation' | relative_url }}) in the
documentation covers the same ground in more depth, including per-project build commands
and the test suite.
