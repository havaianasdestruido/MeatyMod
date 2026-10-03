---
id: mod-manifest
title: Mod manifest
sidebar_label: Mod manifest
description: manifest.json — fields, validation rules and the JSON Schema.
---

# Mod manifest (`manifest.json`)

Every mod directory carries a `manifest.json` at its root describing the mod. It is validated by [`ModManifest.Validate`](../api/meatymod-core.md#modmanifest) and by `manifest.schema.json` at the repository root.

## Example

```json
{
  "Id": "oink",
  "Name": "Oink",
  "Version": "1.0.0",
  "Author": "MeatyMod",
  "Description": "Turn the player into a pig: pig skin texture + sprint speed multiplier (toggle with O).",
  "Replaces": []
}
```

## Fields

| Field | Type | Required | Rule |
| --- | --- | --- | --- |
| `Id` | string | ✅ | non-blank, matches `^[A-Za-z0-9_.-]+$` — letters, digits, `_`, `.`, `-` only |
| `Name` | string | ✅ | non-blank, human-readable |
| `Version` | string | ✅ | parses with `System.Version`: `1.0`, `1.0.0`, `1.0.0.0`. **No** pre-release suffixes |
| `Author` | string | ✅ | non-blank |
| `Description` | string | — | free text |
| `Replaces` | string[] | — | list of asset paths the mod replaces; must not be `null` |

Property names are matched **case-insensitively** when loading (`PropertyNameCaseInsensitive = true`), so `"id"` works, but the schema and every shipped manifest use `PascalCase`. The schema sets `additionalProperties: false` — unknown keys fail schema validation, though the C# loader ignores them.

## Validation messages

| Problem | Message |
| --- | --- |
| missing `Id` | `Id is required.` |
| bad `Id` characters | `Id may only contain letters, digits, underscore, dot, or hyphen (no spaces or slashes).` |
| missing `Name` | `Name is required.` |
| bad `Version` | `Version must be a valid version such as 1.0.0.` |
| missing `Author` | `Author is required.` |
| `Replaces` null | `Replaces must not be null.` |

:::note Why the `Id` grammar is strict
An id with a slash or a space could be interpolated into a path by downstream tooling. Restricting it to `[A-Za-z0-9_.-]` keeps ids safe to use as folder and file names. See the [security model](../architecture/security-model.md#manifest-validation).
:::

## JSON Schema

`manifest.schema.json` (draft-07) mirrors the C# rules and can be wired into your editor for completion and inline errors:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "MeatyMod Manifest",
  "type": "object",
  "required": ["Id", "Name", "Version", "Author"],
  "additionalProperties": false,
  "properties": {
    "Id":          { "type": "string", "minLength": 1, "pattern": "^[A-Za-z0-9_.-]+$" },
    "Name":        { "type": "string", "minLength": 1 },
    "Version":     { "type": "string", "pattern": "^\\d+(\\.\\d+)*$" },
    "Author":      { "type": "string", "minLength": 1 },
    "Description": { "type": "string" },
    "Replaces":    { "type": "array", "items": { "type": "string" } }
  }
}
```

VS Code, for example:

```json
// .vscode/settings.json
{
  "json.schemas": [
    { "fileMatch": ["mods/*/manifest.json"], "url": "./manifest.schema.json" }
  ]
}
```

## Loading it in code

```csharp
using MeatyMod.Core;

var (manifest, errors) = ModManifestLoader.LoadWithValidation(@"mods\Oink\manifest.json");

if (errors.Length > 0)
{
    foreach (var e in errors) { Console.Error.WriteLine(e); }
    return 1;
}

Console.WriteLine($"{manifest!.Name} {manifest.Version} by {manifest.Author}");
```

:::caution The manifest is metadata, not a loader descriptor
Nothing in the injection path reads `manifest.json` — the entry type is discovered from the DLL's IL, not declared here. [`pack`](../cli/pack.md) includes the manifest in the archive and hashes it like any other file, but neither `pack` nor `install` currently *enforces* its presence. Ship one anyway: it is what identifies your mod to humans and to future tooling.
:::

## Related

- [`ModManifest` / `ModManifestLoader` API](../api/meatymod-core.md#modmanifest)
- [Mod package](./mod-package.md) · [Authoring a mod](../mods/authoring-a-mod.md)
