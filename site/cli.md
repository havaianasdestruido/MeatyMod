---
title: Command line
eyebrow: Reference
description: The nine meatymod commands at a glance — syntax, behaviour and exit codes, with links to the full reference for each one.
---

Everything MeatyMod does from the shell goes through one executable:

```text
meatymod <command> [arguments]
```

Running it with no command, or with a command it does not recognise, prints the usage block
and exits with status `1`. Command names are matched exactly and are case-sensitive.

## Commands

| Command | Syntax |
| --- | --- |
| [pack]({{ site.docs_url | append: 'cli/pack' | relative_url }}) | `pack <mod-dir> [output.zip]` |
| [install]({{ site.docs_url | append: 'cli/install' | relative_url }}) | `install <mod.zip> <game-path>` |
| [inject]({{ site.docs_url | append: 'cli/inject' | relative_url }}) | `inject <game.exe> [--mod <dll> [--entry <Type>]]... [output.exe]` |
| [restore]({{ site.docs_url | append: 'cli/restore' | relative_url }}) | `restore <patched.exe>` |
| [manifest]({{ site.docs_url | append: 'cli/manifest' | relative_url }}) | `manifest <content-dir> [out.json]` |
| [verify]({{ site.docs_url | append: 'cli/verify' | relative_url }}) | `verify <xnb-or-dir>` |
| [parse]({{ site.docs_url | append: 'cli/parse' | relative_url }}) | `parse <file>` |
| [xnb]({{ site.docs_url | append: 'cli/xnb' | relative_url }}) | `xnb <file-or-dir>` |
| [checksum]({{ site.docs_url | append: 'cli/checksum' | relative_url }}) | `checksum <file-or-dir>` |

## Packaging

### pack

Zips a mod folder and writes a `checksums.txt` beside its contents. `bin`, `obj` and
dot-prefixed folders are skipped, as is any single file over 100 MiB. The default output is
`.\mod.zip`.

```bat
meatymod pack mods\MyMod mymod.zip
```

Build output is excluded deliberately — a code mod's DLL is deployed by `inject`, not by the
archive.

### install

Verifies the archive's checksums before touching the disk, then extracts into
`<game-path>\Content`. Any hash mismatch aborts the whole install. Entries that would escape
the content directory are refused, oversized entries are skipped, and every replaced file is
copied into `<game-path>\Backups\MeatyMod\` first.

```bat
meatymod install mymod.zip "game\Blood and Bacon"
```

## Patching

### inject

Rewrites the game assembly so each mod DLL's entry method runs when the game constructs
itself. The original executable is copied to `<output>.backup` before anything is written.
Each DLL is copied next to the executable, and its `config.txt` is deployed to both locations
the mods look in.

```bat
meatymod inject "game\Blood and Bacon\BloodandBacon.exe" ^
  --mod Oink.dll ^
  --mod QuackMenu.dll --entry QuackMenu.QuackMenuEntry
```

Without `--entry`, the entry type is auto-detected: any type exposing a static one-argument
`Inject`, preferring names ending in `Entry`.

### restore

```bat
meatymod restore "game\Blood and Bacon\BloodandBacon.exe"
```

Copies `<exe>.backup` back over `<exe>`. The one command worth memorising.

## Inspecting assets

### verify

Validates XNB headers across a file or a whole tree and exits non-zero if anything is invalid
— or if no XNB files were found at all. It accepts Windows platform, format version 5 content;
Xbox 360 and XNA 3.1 files are reported invalid even though they are well-formed.

### xnb

Reports platform, version, compression and sizes for one file, or sweeps a directory and
prints a summary. Directory mode always exits `0`; use `verify` when you need a failing exit
code.

### parse

Dumps a readable view of a loose asset. `.raw` files are read as 16-bit heightmaps with
inferred dimensions; everything else is read as text, and files that look like camera tracks
are decoded into keyframes.

### manifest

Indexes every asset under a content directory into JSON, keyed by file name. Used to diff
installs and to confirm a content mod landed where you expected.

### checksum

Prints `path  sha256` for a file or every file beneath a directory, sorted, followed by a
count. The same format `pack` embeds in archives, so you can diff a deployed install against
a package by hand.

## Exit codes

| Code | Meaning |
| --- | --- |
| `0` | Success. |
| `1` | Usage error, unknown command, or a command-specific failure. |

A command-specific failure prints its reason to **standard error** before returning `1`. Most are one line; a failed integrity check prints a header followed by one line per mismatch.

---

Full syntax, argument tables, worked examples and every error message are in the
[CLI reference]({{ site.docs_url | append: 'cli/overview' | relative_url }}).
