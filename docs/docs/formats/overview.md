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
| Numeric parsing | always `CultureInfo.InvariantCulture` |
| Hashes | lower-case hex SHA-256 |
| Paths inside archives | forward slashes |

## Quick identification

```bat
meatymod verify <path>     :: is it a Windows XNA 4.0 XNB?
meatymod xnb <path>        :: platform, version, flags, sizes, type id
meatymod parse <path>      :: TXT / camera track / RAW heightmap
meatymod checksum <path>   :: SHA-256 of anything
```
