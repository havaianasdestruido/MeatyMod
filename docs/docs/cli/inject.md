---
id: inject
title: meatymod inject
sidebar_label: inject
description: IL-patch one or more mod DLLs into BloodandBacon.exe and deploy their payloads.
---

# `meatymod inject`

Patches the game executable so that it calls each mod's `Inject(Game)` method on startup, then copies the mod DLLs and their configs next to the executable.

```text
meatymod inject <game-exe> [--mod <dll> [--entry <TypeName>]]... [output-exe]
```

:::danger
This runs third-party code inside the game process. Only inject mods you have read or built yourself. See the [security model](../architecture/security-model.md).
:::

## Argument forms

Both of these are accepted:

**Flagged (recommended, supports multiple mods)**

```bat
meatymod inject "game\Blood and Bacon\BloodandBacon.exe" ^
  --mod mods\Oink\src\Oink\bin\Release\net40\Oink.dll ^
  --mod mods\QuackMenu\src\QuackMenu\bin\Release\net40\QuackMenu.dll
```

`--entry <TypeName>` binds to the `--mod` that precedes it:

```bat
meatymod inject game.exe --mod Mod.dll --entry MyMod.CustomEntry
```

**Legacy positional (single mod)**

```bat
meatymod inject <game-exe> <mod-dll> [output-exe] [--entry <TypeName>]
```

Used when no `--mod` flag appears anywhere in the argument list. `--entry` cannot be the second argument in this form (the parser prints the positional usage line and exits `1`).

## Output path and backup

- With one positional argument, the patch is written **in place** over the executable.
- With two or more, the **last** positional argument is the output executable.
- The backup is always `<output-exe>.backup`, overwritten on every run.

```bat
:: in place → game\…\BloodandBacon.exe + BloodandBacon.exe.backup
meatymod inject "game\Blood and Bacon\BloodandBacon.exe" --mod Oink.dll

:: side by side → Patched.exe + Patched.exe.backup
meatymod inject "game\Blood and Bacon\BloodandBacon.exe" --mod Oink.dll "game\Blood and Bacon\Patched.exe"
```

:::caution Restore before re-injecting
The backup is overwritten each time, so injecting into an already-patched executable stores a patched "original". Run [`restore`](./restore.md) first — the shipped `run.bat` / `launch-*.bat` scripts always do.
:::

## What it writes

```text
Patched C:\…\game\Blood and Bacon\BloodandBacon.exe
Backup saved to C:\…\game\Blood and Bacon\BloodandBacon.exe.backup
Mods deployed next to game exe.
```

For each mod DLL, in order:

| File | Source |
| --- | --- |
| `<outputDir>\<Mod>.dll` | the DLL you passed |
| `<outputDir>\<Mod>.txt` | the mod's `config.txt`, if found |
| `<outputDir>\Content\<Mod>\config.txt` | the same file, in the location mods probe first |

`config.txt` is located by walking five directory levels up from the DLL — `bin\<config>\<tfm>` → `src\<Mod>` → mod root — which is exactly the layout of `mods\Oink` and `mods\QuackMenu`. If it is not there, the step is silently skipped.

Two DLLs with the same file name cannot both be deployed; the second prints

```text
Warning: duplicate mod DLL filename Oink.dll, skipping deploy of C:\other\Oink.dll
```

…but **both** calls are still injected into the IL, so rename your DLLs rather than relying on this.

## The IL change

For each mod, two instructions are inserted before the final `ret` of `Blood.myGame`'s instance constructor:

```text
ldarg.0
call <EntryType>::Inject(Microsoft.Xna.Framework.Game)
```

Calls appear in `--mod` order. Entry types are resolved per DLL: `--entry` exact match, else the legacy name `QuackMenu.QuackMenuEntry`, else the first type exposing a `static Inject(x)` method, preferring names ending in `Entry`. Full detail in [Injection pipeline](../architecture/injection-pipeline.md).

## Exit codes and errors

| Code | When |
| --- | --- |
| `0` | patched and deployed |
| `1` | bad arguments, or `Inject failed: <message>` |

Common failures:

```text
Missing DLL path after --mod.
Missing type name after --entry.
No mod DLL specified.
Inject failed: Game executable not found.
Inject failed: Mod DLL not found.
Inject failed: Blood.myGame type not found in game assembly.
Inject failed: Mod entry type not found: MyMod.Nope
Inject failed: MyMod.Entry.Inject(Game) method not found in mod DLL.
```

## Verifying the result

```bat
tools\modharness\bin\Release\net10.0-windows\ModHarness.exe     :: headless proof the mod runs
tools\memscan\bin\Release\net10.0\memscan.exe BloodandBacon     :: proof it loaded in the live game
```

See [ModHarness](../tools/modharness.md) and [memscan](../tools/memscan.md).

## Related

- [`restore`](./restore.md) — undo the patch.
- [Authoring a mod](../mods/authoring-a-mod.md) — the `Inject(Game)` contract.
