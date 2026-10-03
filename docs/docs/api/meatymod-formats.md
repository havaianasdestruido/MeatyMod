---
id: meatymod-formats
title: MeatyMod.Formats
sidebar_label: MeatyMod.Formats
description: API reference for the XNB, LZX, TXT, RAW, camera-track and dialogue parsers.
---

# MeatyMod.Formats

`namespace MeatyMod.Formats` — target `net10.0`, no package references.

Stateless readers for every file format MeatyMod understands. Nothing here validates game semantics; it only turns bytes into structures and throws on malformed input.

| Type | Reads |
| --- | --- |
| [`XnbReader`](#xnbreader) / [`XnbHeader`](#xnbheader) | the five-byte XNB preamble from a stream |
| [`XnbContentReader`](#xnbcontentreader) / [`XnbContent`](#xnbcontent) | the full XNB header plus the LZX-decompressed payload |
| `LzxDecoder` (internal) | the LZX bitstream — see [XNB format](../formats/xnb.md#lzx-decompression) |
| [`TxtReader`](#txtreader) / [`TxtDocument`](#txtdocument) | line-oriented text assets |
| [`RawReader`](#rawreader) / [`RawHeightmap`](#rawheightmap) | 16-bit heightmaps |
| [`CameraTrackParser`](#cameratrackparser) / [`CameraKeyframe`](#camerakeyframe) | camera paths |
| [`DialogueParser`](#dialogueparser) | dialogue line lists |

---

## XnbReader

`public static class XnbReader` — minimal header peek.

### `static XnbHeader ReadHeader(Stream stream)`

Reads 3 magic chars, then one version byte, then one flags byte, leaving the stream open (`BinaryReader(..., leaveOpen: true)`).

**Throws** `ArgumentNullException` for a null stream, `InvalidDataException("Not XNB file.")` when the magic is not `XNB`.

:::caution Offsets differ from `XnbContentReader`
`XnbReader` treats byte 3 as *version* and byte 4 as *flags*. The real XNB layout — implemented by `XnbContentReader` — is magic, **platform**, version, flags. Nothing is skipped; both bytes are read, but under the wrong names: `XnbReader.Version` carries the platform byte (`0x77` `w` for Windows) and `XnbReader.Flags` carries the XNB format version byte (`4` or `5`). The actual flags byte, which holds the `0x80` LZX bit, is never read here. It is retained for the simple "is this XNB" probe; use `XnbContentReader` for real work.
:::

```csharp
using var stream = File.OpenRead(path);
var header = XnbReader.ReadHeader(stream);
Console.WriteLine(header.Magic);    // XNB
```

## XnbHeader

`public class XnbHeader`

| Property | Type |
| --- | --- |
| `Magic` | `string` (always `"XNB"`) |
| `Version` | `byte` |
| `Flags` | `byte` |

---

## XnbContentReader

`public static class XnbContentReader` — the real reader, including decompression.

### `static XnbContent Read(string path)`

Opens the file and parses it. **Throws** `ArgumentNullException` when `path` is null.

Parsing steps:

1. 3 bytes magic — must be `0x58 0x4E 0x42`, else `InvalidDataException("Not XNB file.")`;
2. 1 byte platform (kept verbatim);
3. 1 byte version — only `4` and `5` accepted, else `Unsupported XNB version N.`;
4. 1 byte flags;
5. `int32` total file length — negative ⇒ `Invalid XNB file length.`;
6. if `flags & 0x80` (compressed): `int32` decompressed size (negative ⇒ `Invalid XNB decompressed size.`), then the LZX stream is decoded frame by frame;
7. otherwise the remainder of the file is the payload.

The compressed branch computes the compressed length as `Math.Min(xnbLength - stream.Position, stream.Length - stream.Position)`, so a file whose header over-reports its length is still read safely.

**Decompression failures** throw `InvalidDataException`: `Unexpected end of LZX data.` (truncated) or `LZX decompression failed.` (decoder error, or output length does not match the header).

```csharp
var content = XnbContentReader.Read(@"Content\npc\piggy1.xnb");

Console.WriteLine($"platform 0x{content.Platform:X2}");
Console.WriteLine($"version {content.Version}, flags 0x{content.Flags:X2}");
Console.WriteLine($"{content.Content.Length} payload bytes (compressed={content.IsCompressed})");
```

## XnbContent

`public sealed class XnbContent` — plain public fields (not properties).

| Field | Type | Meaning |
| --- | --- | --- |
| `Magic` | `string` | always `"XNB"` |
| `Platform` | `byte` | `0x77` `w` Windows, `0x78` `x` Xbox 360, `0x6D` `m` Windows Phone |
| `Version` | `byte` | `5` = XNA 4.0, `4` = XNA 3.1 |
| `Flags` | `byte` | `0x80` compressed, `0x01` HiDef profile |
| `DecompressedSize` | `int` | payload size from the header (or the payload length when uncompressed) |
| `IsCompressed` | `bool` | `Flags & 0x80` |
| `Content` | `byte[]` | the decompressed payload, starting at the type-reader table |

---

## TxtReader

`public static class TxtReader` — line-oriented text assets.

### `static TxtDocument Read(string path)`

Reads the file with `File.OpenText`, trims every line and **drops empty lines**, so indices are positional over non-blank content.

### `static TxtDocument Parse(IEnumerable<string> lines)`

Same normalisation for an in-memory sequence — useful in tests.

## TxtDocument

`public sealed class TxtDocument` — an immutable view over the trimmed lines.

| Member | Returns |
| --- | --- |
| `IReadOnlyList<string> Lines` | the normalised lines |
| `int Count` | line count |
| `string GetString(int index)` | the line, or `""` when out of range |
| `bool GetBool(int index)` | `GetInt(index) != 0` |
| `int GetInt(int index)` | parsed int, or `0` |
| `float GetFloat(int index)` | parsed float, or `0f` |
| `bool TryGetInt(int index, out int value)` | `false` when out of range or unparsable |
| `bool TryGetFloat(int index, out float value)` | same, for floats |

Nothing throws on a bad index — out-of-range reads return the type default, matching how the game treats short files. All numeric parsing uses `NumberStyles.Integer` / `NumberStyles.Float` with `CultureInfo.InvariantCulture`.

```csharp
var doc = TxtReader.Read(@"Content\day1.txt");

int waves = doc.GetInt(0);
float spawnRate = doc.GetFloat(1);
bool bossDay = doc.GetBool(2);

if (!doc.TryGetInt(99, out _)) { /* line 99 is absent */ }
```

---

## RawReader

`public static class RawReader` — 16-bit little-endian heightmaps.

### `static RawHeightmap Read(string path, int width = 2048, int height = 2048)`

Reads `width * height` `ushort` samples.

**Throws**

- `ArgumentOutOfRangeException(nameof(width), "Width and height must be positive.")` when either dimension is `<= 0`;
- `IOException("File is too short for a {w}x{h} heightmap: {n} bytes, expected at least {m}.")` when the file cannot supply the required `width * height * 2` bytes.

A file **longer** than required is accepted; the extra bytes are ignored.

### `static bool TryGuessDimensions(string path, out int width, out int height)`

Infers dimensions from the byte length:

1. reject a file shorter than 2 bytes or with an odd length;
2. `samples = length / 2`;
3. if `samples` equals `size * size` for `size` in `{512, 1024, 2000, 2048, 4096}` → square;
4. otherwise, for the same candidate sizes, if `samples % size == 0` → `width = size`, `height = samples / size`;
5. otherwise `false` with both outputs `0`.

```csharp
if (!RawReader.TryGuessDimensions(path, out var w, out var h)) { w = h = 2048; }
var map = RawReader.Read(path, w, h);
Console.WriteLine($"{map.Width}x{map.Height}, {map.Heights.Length} samples");
```

## RawHeightmap

`public sealed class RawHeightmap` — public fields.

| Field | Type |
| --- | --- |
| `Width` | `int` |
| `Height` | `int` |
| `Heights` | `ushort[]` (row-major, defaults to an empty array) |

Index a sample with `Heights[y * Width + x]`.

---

## CameraTrackParser

`public static class CameraTrackParser`

### `static IReadOnlyList<CameraKeyframe> Parse(string path)`

Reads the file line by line, skips blanks, parses each line as a float (`CultureInfo.InvariantCulture`) and emits a keyframe for every **complete group of six** values. Lines that do not parse as floats are ignored, and a trailing partial group is discarded.

## CameraKeyframe

`public struct CameraKeyframe` — public fields.

| Field | Meaning |
| --- | --- |
| `PosX`, `PosY`, `PosZ` | camera position |
| `TargetX`, `TargetY`, `TargetZ` | look-at target |

```csharp
foreach (var kf in CameraTrackParser.Parse(path))
{
    Console.WriteLine($"Pos({kf.PosX}, {kf.PosY}, {kf.PosZ}) Target({kf.TargetX}, {kf.TargetY}, {kf.TargetZ})");
}
```

---

## DialogueParser

`public static class DialogueParser`

### `static IReadOnlyList<string> Parse(string path)`

Returns every non-empty trimmed line, in order. There is no speaker/timing syntax in the game's dialogue assets — they are positional line lists, so this is deliberately the same normalisation `TxtReader` applies, exposed as a plain list.

---

## Error-handling summary

| Reader | On malformed input |
| --- | --- |
| `XnbReader` | throws `InvalidDataException` |
| `XnbContentReader` | throws `InvalidDataException` (magic, version, lengths, LZX) |
| `TxtReader` / `TxtDocument` | never throws on content; out-of-range reads return defaults |
| `RawReader` | throws `ArgumentOutOfRangeException` / `IOException` |
| `CameraTrackParser` | silently skips unparsable lines |
| `DialogueParser` | silently skips blank lines |

Test coverage lives in `XnbReaderTests`, `XnbContentReaderTests`, `LzxFixtureTests` (against the real `Fixtures\darkFog3_0.xnb`), `TxtReaderTests`, `RawReaderTests`, `RawReaderEdgeTests`, `CameraTrackParserTests` and `DialogueParserTests`.
