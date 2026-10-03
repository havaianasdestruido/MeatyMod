---
id: xnb
title: XNB format
sidebar_label: XNB
description: Byte layout of XNA content files, the LZX framing, and how MeatyMod decodes them.
---

# XNB format

XNB is the compiled content format produced by the XNA Content Pipeline. Blood & Bacon ships 1860 of them under `Content\`.

## Header layout

| Offset | Size | Field | Notes |
| --- | --- | --- | --- |
| 0 | 3 | magic | ASCII `XNB` (`0x58 0x4E 0x42`) |
| 3 | 1 | target platform | `w` (`0x77`) Windows, `x` (`0x78`) Xbox 360, `m` (`0x6D`) Windows Phone |
| 4 | 1 | format version | `5` = XNA 4.0, `4` = XNA 3.1 |
| 5 | 1 | flags | bit `0x80` LZX compressed, bit `0x01` HiDef profile |
| 6 | 4 | file size | total size of the XNB file, `int32` little-endian |
| 10 | 4 | decompressed size | **only when** flag `0x80` is set |
| 10 or 14 | … | payload | LZX frames, or the raw content stream |

The payload — once decompressed — starts with the content type-reader table, then the shared-resource count, then the object graph. MeatyMod stops at the raw payload bytes: it does **not** implement the type readers, so it cannot turn an XNB into a `Texture2D` outside the game.

## How MeatyMod reads it

Three readers with different cost/strictness trade-offs:

| Reader | Reads | Accepts | Cost |
| --- | --- | --- | --- |
| `AssetValidator.ValidateXnb` | 5 bytes | **only** `XNB` + `w` + version `5` | trivial |
| `XnbReader.ReadHeader` | 5 bytes | any `XNB` | trivial |
| `XnbContentReader.Read` | whole file | `XNB` + version `4` or `5`, any platform | full decompression |

:::caution `XnbReader` field names are off by one
`XnbReader.ReadHeader` reads the 3 magic chars and then two more bytes, assigning them to `Version` and `Flags` in that order. No byte is skipped — the names are simply shifted against the real layout:

| Property | Byte | What it actually holds |
| --- | --- | --- |
| `Magic` | 0–2 | `XNB` |
| `Version` | 3 | the **platform** byte (`w` / `x` / `m`) |
| `Flags` | 4 | the **XNB format version** byte (`4` or `5`) |

The real flags byte (bit `0x80` = LZX) is never read by `XnbReader`. It is kept for the cheap "is this XNB" probe; use `XnbContentReader` whenever the values matter. See the [API note](../api/meatymod-formats.md#xnbreader).
:::

## LZX decompression

Compressed XNB payloads use the LZX algorithm (the same one used in CAB/CHM), with a 16-bit window, framed as a sequence of blocks.

### Frame headers

`XnbContentReader.DecompressLzx` reads each frame as:

```text
hi, lo = next two bytes

if hi == 0xFF:
    hi, lo       = lo, next byte        → frameSize  = (hi << 8) | lo
    hi, lo       = next two bytes       → blockSize  = (hi << 8) | lo
    pos += 5
else:
    blockSize = (hi << 8) | lo
    frameSize = 0x8000                  (MaxDecodedFrameSize, 32 KiB)
    pos += 2
```

A `blockSize` or `frameSize` of zero ends the stream. After each block the stream is explicitly re-seeked to `pos + blockSize`, so a decoder that over- or under-reads cannot desynchronise the framing.

The loop finishes with a hard check:

```csharp
if (output.Position != decompressedSize) { throw new InvalidDataException("LZX decompression failed."); }
```

### The decoder

`internal class LzxDecoder` (≈640 lines in `src\MeatyMod.Formats\LzxDecoder.cs`) is a C# port of the classic LZX decoder used by the MonoGame/XNA community. It is `internal` on purpose: it is not a general-purpose LZX API and the framing above is part of the contract.

Implementation notes, for anyone maintaining it:

- constructed with a window exponent — `new LzxDecoder(16)` → 64 KiB window; the constructor rejects anything outside 15…21;
- the window buffer is pre-filled with `0xDC` to match reference implementations;
- three Huffman trees (`MAINTREE`, `LENGTH`, `ALIGNED`) plus a `PRETREE` for delta-coded code lengths;
- supports all three LZX block types — verbatim, aligned-offset and uncompressed;
- implements the Intel E8 call translation pass (`intel_filesize`, `intel_curpos`);
- `R0`/`R1`/`R2` repeated-offset slots persist across frames, which is why the decoder instance is reused for the whole file;
- `Decompress` returns non-zero on error, which the caller converts into `InvalidDataException`.

The field and method names deliberately keep the original lower-case/underscore style of the reference implementation rather than the repository's C# conventions, so the port stays diffable against its source.

## Examples

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

```csharp
var content = XnbContentReader.Read(path);
File.WriteAllBytes(path + ".bin", content.Content);   // raw payload dump
```

## Validation errors

| Message | Cause |
| --- | --- |
| `Not XNB file.` | magic mismatch |
| `Unsupported XNB version N.` | version other than `4` or `5` |
| `Invalid XNB file length.` | negative `int32` at offset 6 |
| `Invalid XNB decompressed size.` | negative `int32` at offset 10 |
| `Unexpected end of LZX data.` | stream ended inside a frame header |
| `LZX decompression failed.` | decoder error, or output length did not match the header |

## Test fixture

`src\MeatyMod.Tests\Fixtures\darkFog3_0.xnb` is a real compressed asset from the game, committed so that `LzxFixtureTests` can prove the decoder against ground truth rather than synthetic data. `XnbContentReaderTests` covers the malformed-header paths.

## Related

- [`meatymod xnb`](../cli/xnb.md) · [`meatymod verify`](../cli/verify.md)
- [`MeatyMod.Formats` API](../api/meatymod-formats.md#xnbcontentreader)
