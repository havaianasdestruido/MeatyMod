---
id: manifest
title: meatymod manifest
sidebar_label: manifest
description: Build a basename to path index of every XNB asset in a Content directory.
---

# `meatymod manifest`

Indexes every `.xnb` file under a content directory, keyed by file name without extension.

```text
meatymod manifest <game-content-dir> [out.json]
```

| Argument | Meaning |
| --- | --- |
| `<game-content-dir>` | directory to scan, recursively |
| `[out.json]` | write indented JSON here; omit to print `key -> path` lines to stdout |

## Examples

```bat
meatymod manifest "game\Blood and Bacon\Content"
meatymod manifest "game\Blood and Bacon\Content" assets.json
```

```text
piggy1 -> C:\…\Content\npc\piggy1.xnb
jon6 -> C:\…\Content\texture\jon6.xnb
darkFog3_0 -> C:\…\Content\fx\darkFog3_0.xnb
```

With an output path:

```text
Manifest written to assets.json
```

```json
{
  "piggy1": "C:\\…\\Content\\npc\\piggy1.xnb",
  "jon6": "C:\\…\\Content\\texture\\jon6.xnb"
}
```

Serialization uses `JsonSerializer` with `WriteIndented = true`, so paths are JSON-escaped (doubled backslashes on Windows) and absolute as enumerated.

## Semantics

The implementation is deliberately tiny ([`AssetManifestBuilder.Build`](../api/meatymod-assets.md)):

```csharp
foreach (var file in Directory.EnumerateFiles(gameContentPath, "*.xnb", SearchOption.AllDirectories))
{
    manifest[Path.GetFileNameWithoutExtension(file)] = file;
}
```

:::caution Duplicate basenames collapse
The dictionary is keyed by basename only. If `fx\smoke.xnb` and `npc\smoke.xnb` both exist, the manifest keeps whichever is enumerated last. The key count is therefore the number of **unique basenames**, not the number of files — `tools\smoke.ps1` asserts 1353 keys against 1860 valid XNB files for the shipped game.
:::

Only `*.xnb` is matched; audio banks, shaders and loose text assets are not indexed.

## Exit codes

| Code | When |
| --- | --- |
| `0` | manifest printed or written |
| `1` | no argument, directory not found, or `Manifest failed: <message>` |

## Uses

- A quick "does this asset exist, and where" lookup while reverse-engineering.
- A stable snapshot to diff between game patches — see `.ai/maintenance/GAME_UPDATES.MD` in the repository.
- Input for scripts that drive [`xnb`](./xnb.md) or [`parse`](./parse.md) over selected assets.

## Related

- [`verify`](./verify.md) — validate the same tree.
- [`MeatyMod.Assets` API](../api/meatymod-assets.md).
