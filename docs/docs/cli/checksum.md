---
id: checksum
title: meatymod checksum
sidebar_label: checksum
description: Print SHA-256 hashes for a file or every file under a directory.
---

# `meatymod checksum`

Prints lower-case SHA-256 hashes, in the same format `pack` embeds into `checksums.txt`.

```text
meatymod checksum <file-or-dir>
```

## Single file

```bat
meatymod checksum mods\Oink\manifest.json
```

```text
mods\Oink\manifest.json  5f1d…9a
```

The path is echoed **exactly as you typed it**, then two spaces, then the hash.

## Directory

```bat
meatymod checksum mods\Oink
```

```text
build.bat  3c0a…71
config.txt  9ab0…4e
lib/xna/Microsoft.Xna.Framework.dll  77de…02
manifest.json  5f1d…9a
src/Oink/OinkEntry.cs  1c55…7f
Files: 5
```

For directories:

- every file is included, recursively — **no** `bin`/`obj`/dot-directory filtering (unlike [`pack`](./pack.md));
- paths are relative to the directory you passed, with forward slashes;
- the list is sorted with `StringComparer.Ordinal` on the **full** path, so the order is stable across machines;
- a trailing `Files: <n>` line reports the count.

## Comparing against a packed archive

Because the line format matches `checksums.txt`, you can diff a working tree against a shipped archive:

```powershell
# extract the embedded manifest
Expand-Archive mod.zip -DestinationPath .\unpacked
Get-Content .\unpacked\checksums.txt | Sort-Object > packed.txt

# hash the source tree the same way
meatymod checksum mods\Oink | Select-Object -SkipLast 1 | Sort-Object > source.txt

Compare-Object (Get-Content packed.txt) (Get-Content source.txt)
```

Expect differences for files `pack` excludes (`bin`, `obj`, dot-files) and for `checksums.txt` itself.

## Exit codes

| Code | When |
| --- | --- |
| `0` | every file hashed |
| `1` | no argument, path not found, or at least one file could not be read |

Per-file `IOException` / `UnauthorizedAccessException` are reported as `Failed to hash <file>: <message>` on `stderr`; the walk continues and the final exit code becomes `1`.

## Implementation

`ChecksumUtil.Sha256File` streams the file through `SHA256.HashData` and formats with `Convert.ToHexStringLower` — no full-file buffering, so hashing large assets is memory-safe. See the [`MeatyMod.Core` API](../api/meatymod-core.md#checksumutil).

## Related

- [`pack`](./pack.md) — writes the same list into every archive.
- [`install`](./install.md) — verifies it before extracting.
