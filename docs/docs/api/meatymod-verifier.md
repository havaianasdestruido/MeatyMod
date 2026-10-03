---
id: meatymod-verifier
title: MeatyMod.Verifier
sidebar_label: MeatyMod.Verifier
description: API reference for AssetValidator.
---

# MeatyMod.Verifier

`namespace MeatyMod.Verifier` — target `net10.0`, no package references.

A deliberately cheap "is this really a Windows XNA 4.0 content file" check, used for whole-tree sweeps where decompressing every asset would be far too slow.

---

## AssetValidator

`public static class AssetValidator`

### `static bool ValidateXnb(string path)`

Reads the first five bytes and requires exactly:

| Offset | Required | Meaning |
| --- | --- | --- |
| 0 | `0x58` | `X` |
| 1 | `0x4E` | `N` |
| 2 | `0x42` | `B` |
| 3 | `0x77` | `w` — Windows target platform |
| 4 | `0x05` | XNA Game Studio 4.0 format version |

```csharp
byte[] header = new byte[5];
using (FileStream stream = File.OpenRead(path))
{
    int read = stream.Read(header, 0, header.Length);
    if (read < header.Length) { return false; }
}

return header[0] == 0x58 && header[1] == 0x4E && header[2] == 0x42
    && header[3] == 0x77 && header[4] == 5;
```

A file shorter than five bytes returns `false`. I/O exceptions propagate to the caller — only `ValidateDirectory` swallows them.

:::note Stricter than `XnbContentReader`
This rejects Xbox 360 (`x`) and Windows Phone (`m`) content and XNA 3.1 (`version 4`) files, both of which [`XnbContentReader`](./meatymod-formats.md#xnbcontentreader) happily parses. The strictness is intentional: for Blood & Bacon, anything that is not `XNBw5` is wrong.
:::

### `static (int Valid, int Invalid, int Total) ValidateDirectory(string dir)`

Enumerates `*.xnb` recursively and tallies the results. An `IOException` on an individual file counts as **invalid** and the sweep continues; other exception types propagate.

`Total` is `Valid + Invalid` — i.e. the number of `.xnb` files seen. A directory with no XNB files returns `(0, 0, 0)`, which [`meatymod verify`](../cli/verify.md) treats as a failure.

```csharp
using MeatyMod.Verifier;

var (valid, invalid, total) = AssetValidator.ValidateDirectory(@"game\Blood and Bacon\Content");
Console.WriteLine($"Valid: {valid} Invalid: {invalid} Total: {total}");

if (total == 0 || valid != total) { return 1; }
```

---

## When to use what

| Need | Use |
| --- | --- |
| fast gate over thousands of files | `AssetValidator.ValidateDirectory` |
| header fields (platform, flags, sizes) | [`XnbContentReader.Read`](./meatymod-formats.md#xnbcontentreader) |
| the decompressed payload | `XnbContentReader.Read(...).Content` |

Test coverage: `VerifyCommandTests.cs`.
