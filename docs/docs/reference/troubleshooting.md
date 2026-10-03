---
id: troubleshooting
title: Troubleshooting
sidebar_label: Troubleshooting
description: Symptoms, causes and fixes for build, inject, mod and asset problems.
---

# Troubleshooting

## First moves

```bat
:: 1. always get back to a clean executable
meatymod restore "game\Blood and Bacon\BloodandBacon.exe"

:: 2. did the mod run at all?
type "game\Blood and Bacon\oink.log"
type "game\Blood and Bacon\quackmenu.log"

:: 3. is the content intact?
meatymod verify "game\Blood and Bacon\Content"
```

---

## Injection

### `Inject failed: Blood.myGame type not found in game assembly.`

The file you pointed at is not a Blood & Bacon executable, or it is a different build. Check the path — the default layout is `game\Blood and Bacon\BloodandBacon.exe` — and make sure you are not pointing at an already-corrupted file.

### `Inject failed: Mod DLL not found.`

The DLL path is wrong, or the mod was built in a different configuration than the path assumes. Debug and Release land in different folders:

```text
mods\Oink\src\Oink\bin\Debug\net40\Oink.dll
mods\Oink\src\Oink\bin\Release\net40\Oink.dll
```

### `Inject failed: No mod entry type found in mod DLL: <path>`

No type in the DLL exposes `static Inject` with exactly one parameter. Check the method is `public static`, named exactly `Inject`, and takes one argument. If it is there, pass the type explicitly:

```bat
meatymod inject game.exe --mod MyMod.dll --entry MyMod.MyModEntry
```

### `Inject failed: Mod entry type not found: <name>`

`--entry` is matched against the **full** name (`Namespace.Type`) with no fallback. Typos are fatal by design.

### The wrong entry type was chosen

Auto-detection prefers a type named `QuackMenu.QuackMenuEntry`, then any type with a static one-argument `Inject`, preferring names ending in `Entry`. With several candidates the result depends on module order — pass `--entry`.

### `Restore failed` or the game will not start after restoring

If `BloodandBacon.exe.backup` is itself a patched file (you injected twice without restoring), the backup is useless. Steam → *Properties* → *Installed Files* → *Verify integrity of game files*. Then always restore before re-injecting — the `run.bat` scripts do.

### The executable is locked

Close the game and Steam, then retry. `inject` writes through a temp file for in-place patches, but the final copy still needs exclusive access.

---

## Mods

### Nothing happens in game, and the log file is missing

`Inject` never ran:

1. confirm the executable is patched — compare it to `.backup` with `fc /b`;
2. confirm `<Mod>.dll` sits next to the executable (`inject` copies it; manual deployments must too);
3. run [`ModHarness`](../tools/modharness.md) to prove the mod's code works outside the game;
4. run [`memscan`](../tools/memscan.md) against the live process to see whether the module loaded.

### The log says `ScreenManager not found in Components.`

Expected during the first frames — the mods retry every frame. If it never resolves, the game build changed its type name or structure. Field and type names are listed in [Mod runtime model](../architecture/mod-runtime.md#stage-4--reflection-against-internal-game-types).

### The log says `No BloodnBacon screen active.`

You are at a menu, not in gameplay. Start a game.

### Config changes are ignored

- The file must be where the mod looks: `<game>\Content\<Mod>\config.txt` or `<game>\<modname>.txt`.
- `inject` overwrites both from the repository copy on every run — edit `mods\<Mod>\config.txt` and re-inject, or edit the deployed copy and just relaunch.
- Key names are matched against a `switch` of lower-case literals; an unknown key is silently ignored. `CreativeMode` maps to `CreativeModeEnabled`, not the other way around.
- A malformed value falls back to the default rather than erroring.

### The pig skin looks wrong

Not a bug — it is a UV-layout mismatch between the human model and the pig atlas, documented in detail in [Oink](../mods/oink.md#why-the-pig-skin-looks-misaligned). Set `PigSkin=false` to disable it.

### The QuackMenu overlay opens but has no text

The game content has no sprite font named `QuackMenu` or `Arial`. The log says `No usable sprite font found; menu text will not render.` Navigation still works blind, but you will not see the selection.

### Boss spawning logs `Boss could not be constructed`

The boss class's constructor signature is not one `BuildArgs` can satisfy (it handles `int`, `Blood.ScreenManager`, `string` and `bool`). Unsupported shapes are skipped rather than crashing.

---

## Packing and installing

### `Install aborted: mod integrity check failed.`

The archive's contents do not match its `checksums.txt`. Re-pack from source:

```bat
meatymod pack mods\MyMod
```

If you edited a file inside the zip by hand, the hashes no longer match — that is the guard working.

### `Skipping unsafe entry: <name>`

A zip entry resolves outside `Content\`. The entry is skipped and the rest of the install continues. Rebuild the archive with `pack`, which always writes relative forward-slash paths.

### `Skipping oversized file` / `Skipping oversized entry`

Over the 100 MiB limit (`FileSizeGuard.DefaultMaxBytes`). Split the asset, or raise the constant and rebuild if you genuinely need bigger files.

### The DLL is missing from my packed mod

By design: `pack` excludes `bin\` and `obj\`. Code mods are delivered by building the DLL and running `inject`. See [mod package](../formats/mod-package.md#content-mods-vs-code-mods).

---

## Assets

### `meatymod verify` reports invalid files

`verify` requires exactly `XNB` + `w` + version `5`. Xbox 360 / Windows Phone content and XNA 3.1 files are reported invalid even though they are well-formed. Use `meatymod xnb <file>` to see the real header.

### `No XNB files found (empty content directory?)`

You pointed at a directory with no `.xnb` anywhere beneath it. Note that `verify` exits `1` for this case.

### `Unexpected end of LZX data.` / `LZX decompression failed.`

The file is truncated or corrupt. Verify game files through Steam. If the file is fine in a fresh install and still fails, that is a decoder bug worth reporting with the file attached.

### `Parse failed: File is too short for a 2048x2048 heightmap`

Dimension inference failed and the 2048 × 2048 fallback does not fit. Call `RawReader.Read(path, width, height)` with the real dimensions. The game's own heightmaps (`astro\brushes\earth.raw`, `earth2.raw`) are 8,000,000 bytes = 2000 × 2000.

---

## Build

### `dotnet build` cannot find `net40` reference assemblies

Install the .NET Framework 4.0 targeting pack (Visual Studio installer, or the developer pack). The XNA references themselves are vendored under each mod's `lib\xna\`.

### ModHarness fails to load XNA assemblies

It must be x86 (`PlatformTarget=x86` in `ModHarness.csproj`) because XNA 4.0 is x86-only, and it needs the committed `Microsoft.Xna.Framework.Input.Touch.dll` copied to its output directory. Do not add extra GAC copies.

### The build is full of analyzer warnings

`AnalysisMode=All` enables every rule, with `TreatWarningsAsErrors=false`. Fix them, or suppress narrowly with a justification comment — see [Coding standards](../development/coding-standards.md#analyzers).

---

## Still stuck

Open an issue with the exact command, its full output, the relevant log lines and your SDK version: [github.com/havaianasdestruido/MeatyMod/issues](https://github.com/havaianasdestruido/MeatyMod/issues).
