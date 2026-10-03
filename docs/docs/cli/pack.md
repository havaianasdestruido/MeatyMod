---
id: pack
title: meatymod pack
sidebar_label: pack
description: Pack a mod directory into a zip archive with an embedded SHA-256 manifest.
---

# `meatymod pack`

Zips a mod directory into a distributable archive and embeds a `checksums.txt` listing every packed file.

```text
meatymod pack <mod-directory> [output.zip]
```

| Argument | Required | Default |
| --- | --- | --- |
| `<mod-directory>` | yes | — |
| `[output.zip]` | no | `mod.zip` in the current directory |

## Example

```bat
meatymod pack mods\Oink
meatymod pack mods\QuackMenu dist\quackmenu-1.0.0.zip
```

```text
Packed mods\Oink -> C:\…\MeatyMod\mod.zip
```

## What gets packed

Every file under `<mod-directory>`, recursively, **except**:

| Skipped | Rule |
| --- | --- |
| build output | any path segment equal to `bin` or `obj` (case-insensitive) |
| hidden/tooling files | any path segment starting with `.` (`.git`, `.gitignore`, `.vs`, …) |
| oversized files | larger than `FileSizeGuard.DefaultMaxBytes` (100 MiB) — reported as `Skipping oversized file: <relPath>` on `stderr`, the pack continues |

Paths inside the archive are stored with forward slashes, relative to the mod directory. Compression is `CompressionLevel.Optimal`.

An existing output file is deleted before writing — `pack` never appends to an archive.

## `checksums.txt`

A text entry is added to the archive root with one line per packed file:

```text
manifest.json  7d2f…c1
config.txt  9ab0…4e
src/Oink/OinkEntry.cs  1c55…7f
```

Format: the forward-slash relative path, **two spaces**, then the lower-case SHA-256 of the file (`ChecksumUtil.Sha256File`). It is written with UTF-8 **without** a BOM.

[`install`](./install.md) re-hashes every listed entry and refuses to install on a mismatch. `checksums.txt` describes itself-adjacent files only — it does not include an entry for itself.

:::note Integrity, not authenticity
The manifest lives inside the archive it describes, so it detects corruption and truncation, not tampering by someone who can rebuild the zip. See the [security model](../architecture/security-model.md#integrity-verification-on-install).
:::

## Typical archive layout

```text
mod.zip
├─ checksums.txt        ← added by pack
├─ manifest.json
├─ config.txt
├─ build.bat
├─ run.bat
├─ README.md
├─ lib/xna/*.dll
└─ src/Oink/*.cs
```

Note that `bin\` is excluded, so **the compiled mod DLL is not in the archive**. `pack`/`install` is the content-mod path; code mods are delivered by building the DLL and running [`inject`](./inject.md). See [mod package format](../formats/mod-package.md).

## Exit codes

| Code | When |
| --- | --- |
| `0` | archive written |
| `1` | no arguments, directory not found, or an I/O failure (`Failed to pack mod: <message>`) |

## Related

- [`install`](./install.md) — the consumer of the archive.
- [`checksum`](./checksum.md) — reproduces the same hash list for any directory.
- [Mod package format](../formats/mod-package.md).
