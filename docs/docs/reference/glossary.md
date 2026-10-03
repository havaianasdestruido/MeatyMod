---
id: glossary
title: Glossary
sidebar_label: Glossary
description: Terms used throughout the MeatyMod documentation.
---

# Glossary

### Blood & Bacon

The game MeatyMod targets. A wave-survival FPS built on XNA 4.0 / .NET Framework 4.0, Steam app 434570.

### `Blood.myGame`

The game's `Microsoft.Xna.Framework.Game` subclass. Its instance constructor is the single IL injection point — `inject` appends `Inject(this)` calls just before its final `ret`.

### `Blood.ScreenManager`

A `GameComponent` the game adds to `Game.Components`, holding the screen stack and most of the gameplay state mods care about. Mods find it by full type name.

### `Blood.BloodnBacon`

The active gameplay screen inside `ScreenManager.screens`. Player fields (`myPlayer`, `player1Texture`) live here.

### Code mod

A mod delivered as a .NET DLL and loaded by IL injection. Distributed as a built DLL, installed with [`inject`](../cli/inject.md).

### Content mod

A mod delivered as replacement files under the game's `Content\` folder. Packed with [`pack`](../cli/pack.md), installed with [`install`](../cli/install.md).

### `checksums.txt`

The SHA-256 manifest `pack` embeds in every archive — `<relative/path>  <hash>` per line, UTF-8 without BOM. Verified by `install` before extraction.

### Entry type

The type in a mod DLL exposing `public static void Inject(Game)`. Chosen by `--entry`, or auto-detected — preferring names ending in `Entry`.

### `FileSizeGuard`

The shared 100 MiB per-file limit applied by `pack` and `install`.

### Fail soft

The mods' house rule: a missing or renamed game field is logged and skipped, never thrown. Keeps a game build change from crashing the render loop.

### GameComponent / DrawableGameComponent

XNA base classes that receive `Update` (and `Draw`) callbacks when added to `Game.Components`. How mods get a per-frame hook without patching `Update`.

### HiDef

An XNA graphics profile. Bit `0x01` of the XNB flags byte.

### IL (Intermediate Language)

The bytecode .NET assemblies contain. MeatyMod inserts two IL instructions — `ldarg.0` and `call` — per injected mod.

### In-place patch

An inject whose output path equals its input path. Cecil writes to a GUID-named temp file first, then copies it over the target.

### Injection

Rewriting the game assembly so it calls mod code. In MeatyMod this is strictly a disk-time IL edit via Mono.Cecil, not runtime hooking.

### LZX

The compression algorithm used by compressed XNB payloads, framed in blocks with an optional 5-byte extended header. Decoded by the internal `LzxDecoder`.

### Mod manifest

`manifest.json` — id, name, version, author, description and `Replaces`. Validated by `ModManifest.Validate` and `manifest.schema.json`.

### Mono.Cecil

The assembly-reading/writing library that performs the IL patch. Version 0.11.6; the only third-party runtime dependency of the shipped tool.

### RAW heightmap

A headerless terrain file: 16-bit little-endian samples, row-major. The game's are 2000 × 2000 (8,000,000 bytes).

### Reflection

Reading and writing `internal`/`private` game members at runtime by name, because mods never compile against the game assembly.

### `restore`

Copying `<exe>.backup` back over the executable. The universal undo for injection.

### Shim game

The dynamic `Game` subclass `ModHarness` constructs so a mod's `Inject` can be executed without launching the real game.

### TXT asset

A loose plain-text game asset whose meaning is **positional** — line *n* means whatever the loader says. Day files, camera tracks and dialogue.

### `UpdateOrder`

The XNA property deciding when a component's `Update` runs relative to others. Oink uses `int.MaxValue` (after the game), QuackMenu `int.MinValue` (before it).

### XNA 4.0

Microsoft's retired game framework the game is built on. Its assemblies are x86-only, which is why `ModHarness` targets x86.

### XNB

The compiled content format produced by the XNA Content Pipeline: `XNB` magic, platform byte, version byte, flags byte, lengths, then an optionally LZX-compressed payload.

### Zip slip

The archive attack where an entry path escapes the extraction root (`..\..\windows\…`). Blocked by `InstallCommand.IsInside`.
