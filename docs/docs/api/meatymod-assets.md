---
id: meatymod-assets
title: MeatyMod.Assets
sidebar_label: MeatyMod.Assets
description: API reference for AssetManifestBuilder.
---

# MeatyMod.Assets

`namespace MeatyMod.Assets` — target `net10.0`, no package references.

The smallest project in the suite: one static class that indexes a `Content` tree.

---

## AssetManifestBuilder

`public static class AssetManifestBuilder`

### `static IDictionary<string, string> Build(string gameContentPath)`

Walks `gameContentPath` recursively for `*.xnb` and returns a dictionary of **file name without extension → full path**.

```csharp
var manifest = new Dictionary<string, string>();

foreach (var file in Directory.EnumerateFiles(gameContentPath, "*.xnb", SearchOption.AllDirectories))
{
    manifest[Path.GetFileNameWithoutExtension(file)] = file;
}

return manifest;
```

| Aspect | Behaviour |
| --- | --- |
| Pattern | `*.xnb` only |
| Recursion | `SearchOption.AllDirectories` |
| Key | `Path.GetFileNameWithoutExtension(file)` |
| Value | the path as enumerated (absolute if you passed an absolute root) |
| Duplicate keys | **last writer wins** — the dictionary indexer overwrites |
| Comparer | default ordinal, so keys are case-sensitive |
| Missing directory | `DirectoryNotFoundException` from `EnumerateFiles` |

:::caution Basename collisions
Two assets with the same file name in different folders collapse into one entry. For the shipped game this reduces 1860 XNB files to 1353 unique basenames — the figure `tools\smoke.ps1` asserts. If you need a complete inventory, use [`meatymod checksum`](../cli/checksum.md) on the content directory instead.
:::

### Usage

```csharp
using MeatyMod.Assets;

var manifest = AssetManifestBuilder.Build(@"game\Blood and Bacon\Content");

if (manifest.TryGetValue("piggy1", out var path))
{
    Console.WriteLine(path);   // …\Content\npc\piggy1.xnb
}

File.WriteAllText("assets.json",
    JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
```

The CLI wrapper is [`meatymod manifest`](../cli/manifest.md), which adds the console/JSON output and the error handling. A sample of its output is committed at `src\MeatyMod.Assets.json`.
