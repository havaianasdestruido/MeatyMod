---
id: overview
title: CLI overview
sidebar_label: Overview
description: The meatymod command line — usage, command table, exit codes and conventions.
---

# CLI overview

`meatymod` is a single executable built from `src\MeatyMod.Cli` (`AssemblyName` = `meatymod`). It takes a command as its first argument and forwards the rest.

```text
Usage: meatymod <command> [options]

Commands:
  pack
  install
  inject
  restore
  manifest
  verify
  parse
  xnb
  checksum
```

Running it with no arguments — or with an unknown command — prints exactly that block and exits with code `1`.

## Commands

| Command | Signature | Purpose |
| --- | --- | --- |
| [`pack`](./pack.md) | `pack <mod-directory> [output.zip]` | zip a mod directory and embed `checksums.txt` |
| [`install`](./install.md) | `install <mod-zip> <game-path>` | verify and extract a mod zip into `<game>\Content` |
| [`inject`](./inject.md) | `inject <game-exe> [--mod <dll> [--entry <Type>]]… [output-exe]` | IL-patch mods into the game executable |
| [`restore`](./restore.md) | `restore <patched-exe>` | copy `<exe>.backup` back over the executable |
| [`manifest`](./manifest.md) | `manifest <game-content-dir> [out.json]` | build a basename → path index of every `.xnb` |
| [`verify`](./verify.md) | `verify <xnb-file-or-dir>` | validate XNB headers |
| [`parse`](./parse.md) | `parse <file>` | parse a TXT asset (day / camera / dialogue) or a `.raw` heightmap |
| [`xnb`](./xnb.md) | `xnb <xnb-file-or-dir>` | dump XNB header fields and compression state |
| [`checksum`](./checksum.md) | `checksum <file-or-dir>` | print SHA-256 for a file or a whole tree |

## Exit codes

| Code | Meaning |
| --- | --- |
| `0` | success — the work was done, or the check passed |
| `1` | usage error, missing path, operation failure, **or a failed check** |

`verify` is the one place where `1` routinely means "your data is wrong" rather than "the tool broke": it returns `1` when any XNB is invalid and when a directory contains no XNB files at all.

## Conventions

**Output streams.** Normal results go to `stdout`; usage, warnings and failures go to `stderr`. Pipe-friendly commands (`checksum`, `manifest` without an output path, `xnb`) emit one record per line.

**Paths.** Relative paths are resolved against the current directory. `pack` defaults its output to `.\mod.zip`. Windows-style backslashes are used throughout the docs; forward slashes work too.

**Quoting.** The game folder contains a space — always quote it:

```bat
meatymod verify "game\Blood and Bacon\Content"
```

**Line continuation.** In `cmd.exe` use `^`, in PowerShell use a backtick:

```bat
meatymod inject "game\Blood and Bacon\BloodandBacon.exe" ^
  --mod mods\Oink\src\Oink\bin\Release\net40\Oink.dll
```

**No global flags.** There is no `--help`, `--verbose` or `--version` switch. Each command prints its own usage line to `stderr` when called with too few arguments. The version lives in `MeatyMod.Core.VersionInfo.Version` (`1.0.0`) and is read by `tools\release.ps1` when naming the release archive.

## Running without installing

```bat
dotnet run --project src\MeatyMod.Cli --no-build -- <command> [args]
```

Everything after `--` is passed through verbatim.

## Adding a command

1. Add a class in `src\MeatyMod.Cli\Commands\` implementing [`ICommand`](../api/meatymod-cli.md#icommand).
2. Return `0` / `1`; print usage to `stderr` when arguments are missing.
3. Register it in the list in `Program.Main` — dispatch is a literal list, there is no reflection-based discovery.
4. Add tests next to the existing command tests in `src\MeatyMod.Tests`.
5. Document it under `docs/docs/cli/` and add it to the sidebar.
