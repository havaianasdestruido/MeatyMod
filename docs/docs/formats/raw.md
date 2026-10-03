---
id: raw
title: RAW heightmaps
sidebar_label: RAW heightmaps
description: 16-bit little-endian heightmap files, dimension inference and the reader's bounds checks.
---

# RAW heightmaps

`.raw` files are headerless terrain heightmaps: a flat array of 16-bit unsigned samples in row-major order. Because there is no header, the dimensions have to come from somewhere else — the loader, or an educated guess.

## Layout

| Property | Value |
| --- | --- |
| Header | none |
| Sample type | `ushort` (16-bit unsigned) |
| Byte order | little-endian |
| Order | row-major — `Heights[y * Width + x]` |
| Expected size | `width * height * 2` bytes |
| Value range | `0`…`65535`, scaled to world height by the game |

## Reading

```csharp
using MeatyMod.Formats;

if (!RawReader.TryGuessDimensions(path, out var w, out var h))
{
    w = h = 2048;                       // the suite's default assumption
}

RawHeightmap map = RawReader.Read(path, w, h);

ushort sample = map.Heights[y * map.Width + x];
```

`RawReader.Read` throws if the file cannot supply `width * height * 2` bytes:

```text
File is too short for a 2048x2048 heightmap: 1048576 bytes, expected at least 8388608.
```

A file **larger** than required is accepted and the surplus ignored. Non-positive dimensions throw `ArgumentOutOfRangeException`.

## Dimension inference

`RawReader.TryGuessDimensions` tries, in order:

1. reject files shorter than 2 bytes or with an odd byte count;
2. `samples = length / 2`;
3. **square match** — `samples == size * size` for `size` in `512, 1024, 2000, 2048, 4096`;
4. **rectangular match** — for the same candidates, the first `size` that divides `samples` evenly becomes the width, with `height = samples / size`;
5. otherwise return `false` with both outputs `0`.

| File size | Samples | Result |
| --- | --- | --- |
| 8,388,608 B | 4,194,304 | 2048 × 2048 (square) |
| 2,097,152 B | 1,048,576 | 1024 × 1024 (square) |
| 8,000,000 B | 4,000,000 | 2000 × 2000 (square) |
| 1,048,576 B | 524,288 | 512 × 1024 (rectangular — 512 divides evenly first) |
| odd size | — | `false` |

:::caution The rectangular branch is a guess
Candidate sizes are tried in ascending order, so an ambiguous sample count resolves to the **smallest** matching width. Always pass explicit dimensions to `Read` when you know them.
:::

## CLI

```bat
meatymod parse "game\Blood and Bacon\Content\terrain\heights.raw"
```

```text
Width: 2048
Height: 2048
SampleCount: 4194304
MinHeight: 0
MaxHeight: 61440
```

`parse` guesses the dimensions, falls back to 2048 × 2048, then scans every sample for the min and max. There is no export to an image format — use the API if you need one:

```csharp
var map = RawReader.Read(path, 2048, 2048);

using var bmp = new Image<L16>(map.Width, map.Height);   // any imaging library
for (var y = 0; y < map.Height; y++)
for (var x = 0; x < map.Width; x++)
{
    bmp[x, y] = new L16(map.Heights[(y * map.Width) + x]);
}
```

## Related

- [`meatymod parse`](../cli/parse.md)
- [`RawReader` API](../api/meatymod-formats.md#rawreader)
- Test coverage: `RawReaderTests.cs`, `RawReaderEdgeTests.cs`
