---
id: meatymod-core
title: MeatyMod.Core
sidebar_label: MeatyMod.Core
description: API reference for ModManifest, ModManifestLoader, BackupManager, ChecksumUtil, FileSizeGuard and VersionInfo.
---

# MeatyMod.Core

`namespace MeatyMod.Core` — target `net10.0`, no package references, `Nullable=enable`.

The shared primitives every other project leans on: the mod manifest model, SHA-256 helpers, a containment-checked backup mirror, the file-size policy and the product version constant.

---

## ModManifest

`public class ModManifest` — the deserialized form of a mod's `manifest.json`.

### Properties

| Property | Type | Default | Notes |
| --- | --- | --- | --- |
| `Id` | `string` | `""` | required; must match `^[A-Za-z0-9_.-]+$` |
| `Name` | `string` | `""` | required, non-blank |
| `Version` | `string` | `""` | required; must parse with `System.Version` |
| `Author` | `string` | `""` | required, non-blank |
| `Description` | `string` | `""` | optional |
| `Replaces` | `List<string>` | empty list | optional; must not be null |

### `bool IsValid()`

Returns `true` when `Validate()` yields no messages. Implemented as `!Validate().Any()`.

### `IEnumerable<string> Validate()`

Lazily yields a human-readable message per problem. The exact strings (asserted by the test suite):

| Condition | Message |
| --- | --- |
| `Id` blank | `Id is required.` |
| `Id` fails the pattern | `Id may only contain letters, digits, underscore, dot, or hyphen (no spaces or slashes).` |
| `Name` blank | `Name is required.` |
| `Version` blank or unparsable | `Version must be a valid version such as 1.0.0.` |
| `Author` blank | `Author is required.` |
| `Replaces` null | `Replaces must not be null.` |

The id pattern is a `static readonly Regex` compiled once with `RegexOptions.Compiled`. `Version` accepts anything `System.Version.TryParse` accepts — `1.0`, `1.0.0` and `1.0.0.0` all pass; `1.0.0-beta` does not.

```csharp
var manifest = new ModManifest { Id = "my mod", Name = "My Mod", Version = "1.0.0", Author = "me" };

foreach (var error in manifest.Validate())
{
    Console.Error.WriteLine(error);
}
// Id may only contain letters, digits, underscore, dot, or hyphen (no spaces or slashes).
```

See the [mod manifest format](../formats/mod-manifest.md) and `manifest.schema.json` at the repository root.

---

## ModManifestLoader

`public static class ModManifestLoader` — JSON loading with `PropertyNameCaseInsensitive = true`.

### `static ModManifest? Load(string path)`

Returns the deserialized manifest, or `null` if the file does not exist **or** anything throws. Never throws, never validates.

### `static (ModManifest? Manifest, string[] Errors) LoadWithValidation(string path)`

The preferred entry point. Returns both the manifest and its validation errors.

| Situation | Result |
| --- | --- |
| file missing | `(null, ["Manifest file not found: <path>"])` |
| JSON deserializes to null | `(null, ["Manifest could not be deserialized."])` |
| malformed JSON | `(null, ["Invalid manifest JSON: <message>"])` (catches `JsonException`) |
| unreadable file | `(null, ["Could not read manifest: <message>"])` (catches `IOException`) |
| parsed | `(manifest, manifest.Validate().ToArray())` — the array is empty when valid |

```csharp
var (manifest, errors) = ModManifestLoader.LoadWithValidation(@"mods\Oink\manifest.json");

if (errors.Length > 0)
{
    foreach (var e in errors) { Console.Error.WriteLine(e); }
    return 1;
}

Console.WriteLine($"{manifest!.Id} {manifest.Version}");
```

:::note A non-null manifest can still be invalid
`LoadWithValidation` returns the parsed object together with its errors — check `Errors.Length`, not `Manifest is not null`.
:::

---

## BackupManager

`public class BackupManager(string backupRoot)` — a primary-constructor class that mirrors files into a backup root, with containment checks.

The root is normalised once: `_backupRoot = Path.GetFullPath(backupRoot)`.

:::caution Paths are relative to the current directory
Both methods compute `Path.GetRelativePath(Directory.GetCurrentDirectory(), <full path>)` and mirror **that** under the backup root. Changing the process working directory between a backup and a restore changes where the file is looked for.
:::

### `void BackupFile(string sourcePath)`

Copies `sourcePath` to `<backupRoot>\<path relative to CWD>`, creating directories as needed. The source is opened with `FileShare.Read`, the destination with `FileShare.None` and `FileMode.Create`.

**Throws**

- `ArgumentException("Cannot back up the working directory itself: <path>")` when the relative path is empty or `.`;
- `ArgumentException("Path resolves outside backup root: <path>")` when the mapping escapes the root;
- the usual `IOException` / `UnauthorizedAccessException` family.

### `void RestoreFile(string gamePath)`

The inverse: reads `<backupRoot>\<path relative to CWD>` and writes it back to `gamePath`, creating the destination directory first. Same exceptions.

```csharp
var backups = new BackupManager(Path.Combine(gameDir, "Backups", "MeatyMod"));
backups.BackupFile(assetPath);
// … modify assetPath …
backups.RestoreFile(assetPath);
```

:::info Not used by `install`
[`InstallCommand`](./meatymod-cli.md#command-implementations) implements its own backup copy inline against `<game>\Backups\MeatyMod`. `BackupManager` is the reusable API — used by the test suite and available to your own tooling.
:::

---

## ChecksumUtil

`public static class ChecksumUtil` — lower-case hex SHA-256.

### `static string Sha256File(string path)`

Opens the file with `File.OpenRead` and streams it through `SHA256.HashData(stream)`, so memory use is constant regardless of file size.

### `static string Sha256(byte[] data)`

Same formatting for an in-memory buffer.

Both return `Convert.ToHexStringLower(...)` — 64 lower-case hex characters, no separators, no prefix. This is the exact format written to `checksums.txt` and printed by [`meatymod checksum`](../cli/checksum.md).

```csharp
Console.WriteLine(ChecksumUtil.Sha256File(@"mods\Oink\config.txt"));
Console.WriteLine(ChecksumUtil.Sha256(Encoding.UTF8.GetBytes("hello")));
```

---

## FileSizeGuard

`public static class FileSizeGuard` — the single size policy shared by `pack` and `install`.

### `const long DefaultMaxBytes = 100L * 1024 * 1024`

100 MiB (104,857,600 bytes).

### `static bool IsAllowed(long length)`

Equivalent to `IsAllowed(length, DefaultMaxBytes)`.

### `static bool IsAllowed(long length, long maxBytes)`

Returns `length <= maxBytes`. A `maxBytes` of zero or less is replaced by `DefaultMaxBytes`, so the guard cannot be switched off by passing `0`.

| Caller | Measures |
| --- | --- |
| `PackCommand` | `new FileInfo(file).Length` before adding to the archive |
| `InstallCommand` | `entry.Length` (the declared uncompressed size) before extracting |

---

## VersionInfo

`public static class VersionInfo`

### `const string Version = "1.0.0"`

The product version. `tools\release.ps1` parses this constant straight out of `VersionInfo.cs` with a regex to name the release archive (`dist\meatymod-1.0.0.zip`), so **bump it here first** when cutting a release.

---

## Test coverage

`src\MeatyMod.Tests` exercises this project from `CoreTests.cs`, `ManifestValidationTests.cs` (14 cases over the manifest rules), `ChecksumTests.cs`, `BackupManagerTests.cs` (including the outside-root rejection) and `InstallChecksumTests.cs`. See [Testing](../development/testing.md).
