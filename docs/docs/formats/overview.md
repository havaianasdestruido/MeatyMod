---
id: overview
title: File formats
sidebar_label: Overview
description: Every file format MeatyMod reads or writes, and which tool handles it.
---

# File formats

MeatyMod touches two families of format: the **game's** asset formats (which it only reads) and **MeatyMod's own** mod packaging formats (which it reads and writes).

## Game assets (read-only)

| Format | Extension | Reader | CLI |
| --- | --- | --- | --- |
| [XNB](./xnb.md) | `.xnb` | `XnbContentReader`, `XnbReader`, `AssetValidator` | [`xnb`](../cli/xnb.md), [`verify`](../cli/verify.md), [`manifest`](../cli/manifest.md) |
| [TXT assets](./txt.md) | `.txt` | `TxtReader`, `CameraTrackParser`, `DialogueParser` | [`parse`](../cli/parse.md) |
| [RAW heightmaps](./raw.md) | `.raw` | `RawReader` | [`parse`](../cli/parse.md) |

:::info The game directory is read-only
MeatyMod never writes into `Content\` except through [`install`](../cli/install.md), and never rewrites an asset in place. Asset authoring (re-encoding an XNB) is out of scope — the suite decodes, it does not encode.
:::

## MeatyMod formats (read-write)

| Format | File | Written by | Read by |
| --- | --- | --- | --- |
| [Mod manifest](./mod-manifest.md) | `manifest.json` | mod author | `ModManifestLoader` |
| [Mod package](./mod-package.md) | `mod.zip` + `checksums.txt` | [`pack`](../cli/pack.md) | [`install`](../cli/install.md) |
| [Mod config](./mod-config.md) | `config.txt` | mod author | the mod itself at runtime |
| Asset manifest | `assets.json` | [`manifest`](../cli/manifest.md) | your tooling |

## Endianness and encoding

| Aspect | Convention |
| --- | --- |
| Binary integers | little-endian (`BinaryReader` defaults; XNA content is little-endian on Windows) |
| Heightmap samples | little-endian `ushort` |
| Text assets | read with `File.OpenText` (UTF-8 with BOM detection) |
| `checksums.txt` | UTF-8 **without** BOM |
| Numeric parsing | format readers pass `CultureInfo.InvariantCulture` explicitly (`TxtDocument.TryGetInt` / `TryGetFloat`, `CameraTrackParser`) |
| Hashes | lower-case hex SHA-256 |
| Paths inside archives | forward slashes |

:::note One exception to the parsing rule
The `All values numeric: True` line printed by [`parse`](../cli/parse.md) is a diagnostic, not a reader. `ParseCommand.Run` tests each line with the culture-sensitive `int.TryParse(line, out _)` / `float.TryParse(line, out _)` overloads rather than the invariant ones. It makes no observable difference today, because `Directory.Build.props` sets `InvariantGlobalization=true` and the current culture *is* the invariant culture — but do not copy that call shape into a reader, where the setting might not hold.
:::

## Quick identification

```bat
meatymod verify <path>     :: is it a Windows XNA 4.0 XNB?
meatymod xnb <path>        :: platform, version, flags, sizes, type id
meatymod parse <path>      :: TXT / camera track / RAW heightmap
meatymod checksum <path>   :: SHA-256 of anything
```
