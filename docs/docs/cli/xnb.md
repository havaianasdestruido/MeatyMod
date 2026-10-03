---
id: xnb
title: meatymod xnb
sidebar_label: xnb
description: Dump XNB header fields, decompress LZX payloads and sweep a content tree.
---

# `meatymod xnb`

Reads XNB files properly — header fields, compression flag, decompressed size — using the full [`XnbContentReader`](../api/meatymod-formats.md#xnbcontentreader), including LZX decompression.

```text
meatymod xnb <xnb-file-or-dir>
```

## Single file

```bat
meatymod xnb "game\Blood and Bacon\Content\npc\piggy1.xnb"
```

```text
Magic: XNB
Platform: 0x77 (w)
Version: 5
Flags: 0x80
Compressed: True
DecompressedSize: 480512
ContentBytes: 480512
TypeId: 04 15 54 65
```

| Field | Source |
| --- | --- |
| `Magic` | always `XNB` (a mismatch throws) |
| `Platform` | byte 3, printed as hex and as its ASCII character — `w` Windows, `x` Xbox 360, `m` Windows Phone |
| `Version` | byte 4 — `5` (XNA 4.0) and `4` (XNA 3.1) are accepted |
| `Flags` | byte 5 — bit `0x80` = LZX compressed, bit `0x01` = HiDef profile |
| `Compressed` | `Flags & 0x80` |
| `DecompressedSize` | the uncompressed payload length |
| `ContentBytes` | bytes actually produced — equals `DecompressedSize` on success |
| `TypeId` | first four payload bytes, printed only for compressed files |

Note that the payload **is** decompressed to produce these numbers: this command is slower than [`verify`](./verify.md) and will fail loudly on a corrupt stream.

## Directory sweep

```bat
meatymod xnb "game\Blood and Bacon\Content\npc"
```

```text
piggy1.xnb: platform 0x77 version 5 flags 0x80 compressed=True
boar1.xnb: platform 0x77 version 5 flags 0x80 compressed=True
Total: 128 Compressed: 128 Failed: 0
```

Paths are printed relative to the directory you passed. A file that fails to read prints `Failed: <path>: <message>` on `stderr`, increments the failure counter and the sweep continues.

## Exit codes

| Code | When |
| --- | --- |
| `0` | file dumped, or sweep completed — **even if some files failed** |
| `1` | no argument, path not found, or a single-file read threw (`Xnb failed: <message>`) |

:::caution The sweep always returns 0
Check the `Failed:` count, not the exit code, when scanning a directory. Use [`verify`](./verify.md) if you want a non-zero exit on bad content.
:::

## Errors you may see

| Message | Meaning |
| --- | --- |
| `Not XNB file.` | the first three bytes are not `XNB` |
| `Unsupported XNB version N.` | only `4` and `5` are handled |
| `Invalid XNB file length.` / `Invalid XNB decompressed size.` | negative length in the header |
| `Unexpected end of LZX data.` | truncated compressed stream |
| `LZX decompression failed.` | the decoder errored, or the output length did not match the header |

## Related

- [XNB format](../formats/xnb.md) — the byte layout and the LZX framing.
- [`verify`](./verify.md) — fast header-only sweep with a meaningful exit code.
