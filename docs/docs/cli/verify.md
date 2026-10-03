---
id: verify
title: meatymod verify
sidebar_label: verify
description: Validate XNB file headers across a file or a whole content tree.
---

# `meatymod verify`

Checks that files really are XNA content files by inspecting the first five header bytes.

```text
meatymod verify <xnb-file-or-dir>
```

## Examples

Single file:

```bat
meatymod verify "game\Blood and Bacon\Content\npc\piggy1.xnb"
```

```text
Valid: True
```

Whole tree:

```bat
meatymod verify "game\Blood and Bacon\Content"
```

```text
Valid: 1860 Invalid: 0 Total: 1860
```

## What "valid" means

[`AssetValidator.ValidateXnb`](../api/meatymod-verifier.md) reads five bytes and requires exactly:

| Offset | Byte | Meaning |
| --- | --- | --- |
| 0 | `0x58` `X` | magic |
| 1 | `0x4E` `N` | magic |
| 2 | `0x42` `B` | magic |
| 3 | `0x77` `w` | target platform — **Windows only** |
| 4 | `0x05` | XNA Game Studio 4.0 format version |

A file shorter than five bytes is invalid. Nothing else is examined — not the flags byte, not the length field, not the payload. This is a cheap sanity sweep, not a decoder; use [`xnb`](./xnb.md) when you need the real header and LZX decompression.

:::note Platform byte
Because byte 3 must be `w`, Xbox 360 (`x`) and Windows Phone (`m`) content is reported invalid even though it is well-formed XNB. Blood & Bacon ships Windows content exclusively.
:::

For directories, `ValidateDirectory` walks `*.xnb` recursively and tallies results; an `IOException` on an individual file counts as invalid rather than aborting the sweep.

## Exit codes

| Code | When |
| --- | --- |
| `0` | single file valid, **or** every XNB in the directory valid |
| `1` | file invalid; any invalid file in a directory; path not found; **no XNB files found at all**; or `Verify failed: <message>` |

The empty-directory case also prints:

```text
No XNB files found (empty content directory?)
```

That makes `verify` usable as a CI gate: a non-zero exit means "do not ship this content tree".

## Using it in a script

```powershell
& meatymod verify "game\Blood and Bacon\Content"
if ($LASTEXITCODE -ne 0) { throw "content verification failed" }
```

`tools\smoke.ps1` does exactly this and additionally asserts the expected counts (`Valid: 1860`, `Invalid: 0`) for a stock install.

## Related

- [`xnb`](./xnb.md) — full header dump and compression state.
- [XNB format](../formats/xnb.md).
