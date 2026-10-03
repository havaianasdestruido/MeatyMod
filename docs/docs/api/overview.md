---
id: overview
title: API reference
sidebar_label: Overview
description: Index of every public type in the MeatyMod libraries.
---

# API reference

Hand-written reference for every public type in the suite, grouped by project. Signatures are transcribed from the source; anything marked *throws* propagates to the caller.

| Project | Namespace | TFM | Package refs |
| --- | --- | --- | --- |
| [MeatyMod.Core](./meatymod-core.md) | `MeatyMod.Core` | `net10.0` | none |
| [MeatyMod.Formats](./meatymod-formats.md) | `MeatyMod.Formats` | `net10.0` | none |
| [MeatyMod.Assets](./meatymod-assets.md) | `MeatyMod.Assets` | `net10.0` | none |
| [MeatyMod.Verifier](./meatymod-verifier.md) | `MeatyMod.Verifier` | `net10.0` | none |
| [MeatyMod.Injector](./meatymod-injector.md) | `MeatyMod.Injector` | `net10.0` | `Mono.Cecil` 0.11.6 |
| [MeatyMod.Cli](./meatymod-cli.md) | `MeatyMod.Cli`, `MeatyMod.Cli.Commands` | `net10.0` | all of the above |

## Type index

| Type | Kind | Project |
| --- | --- | --- |
| `AssemblyInjector` | static class | [Injector](./meatymod-injector.md#assemblyinjector) |
| `AssetManifestBuilder` | static class | [Assets](./meatymod-assets.md#assetmanifestbuilder) |
| `AssetValidator` | static class | [Verifier](./meatymod-verifier.md#assetvalidator) |
| `BackupManager` | class | [Core](./meatymod-core.md#backupmanager) |
| `CameraKeyframe` | struct | [Formats](./meatymod-formats.md#camerakeyframe) |
| `CameraTrackParser` | static class | [Formats](./meatymod-formats.md#cameratrackparser) |
| `ChecksumUtil` | static class | [Core](./meatymod-core.md#checksumutil) |
| `DialogueParser` | static class | [Formats](./meatymod-formats.md#dialogueparser) |
| `FileSizeGuard` | static class | [Core](./meatymod-core.md#filesizeguard) |
| `ICommand` | interface | [Cli](./meatymod-cli.md#icommand) |
| `ModManifest` | class | [Core](./meatymod-core.md#modmanifest) |
| `ModManifestLoader` | static class | [Core](./meatymod-core.md#modmanifestloader) |
| `Program` | static class | [Cli](./meatymod-cli.md#program) |
| `RawHeightmap` | sealed class | [Formats](./meatymod-formats.md#rawheightmap) |
| `RawReader` | static class | [Formats](./meatymod-formats.md#rawreader) |
| `TxtDocument` | sealed class | [Formats](./meatymod-formats.md#txtdocument) |
| `TxtReader` | static class | [Formats](./meatymod-formats.md#txtreader) |
| `VersionInfo` | static class | [Core](./meatymod-core.md#versioninfo) |
| `XnbContent` | sealed class | [Formats](./meatymod-formats.md#xnbcontent) |
| `XnbContentReader` | static class | [Formats](./meatymod-formats.md#xnbcontentreader) |
| `XnbHeader` | class | [Formats](./meatymod-formats.md#xnbheader) |
| `XnbReader` | static class | [Formats](./meatymod-formats.md#xnbreader) |

Command classes (`PackCommand`, `InstallCommand`, `InjectCommand`, `RestoreCommand`, `ManifestCommand`, `VerifyCommand`, `ParseCommand`, `XnbCommand`, `ChecksumCommand`) are listed in the [CLI API page](./meatymod-cli.md#command-implementations). `LzxDecoder` is `internal` to `MeatyMod.Formats` and is documented in the [XNB format](../formats/xnb.md#lzx-decompression) page.

## Referencing the libraries

```bat
dotnet add <your.csproj> reference src\MeatyMod.Core\MeatyMod.Core.csproj
dotnet add <your.csproj> reference src\MeatyMod.Formats\MeatyMod.Formats.csproj
```

There are no NuGet packages; the suite is consumed as project references or by shelling out to `meatymod.exe`.

```csharp
using MeatyMod.Core;
using MeatyMod.Formats;

var (manifest, errors) = ModManifestLoader.LoadWithValidation(@"mods\Oink\manifest.json");
if (errors.Length == 0)
{
    Console.WriteLine($"{manifest!.Name} {manifest.Version} by {manifest.Author}");
}

var xnb = XnbContentReader.Read(@"game\Blood and Bacon\Content\npc\piggy1.xnb");
Console.WriteLine($"{xnb.Content.Length} bytes, compressed={xnb.IsCompressed}");
```

## Conventions across the libraries

- **Static-first.** Only `BackupManager`, `ModManifest`, `TxtDocument`, `RawHeightmap`, `XnbContent`, `XnbHeader` and the command classes are instantiable; everything else is a static helper.
- **No logging.** Libraries never write to the console — the CLI owns all output.
- **Nullability.** `Core`, `Formats`, `Assets` and `Verifier` are compiled with `Nullable=enable`. `Cli` and `Injector` set `Nullable=disable`, so their signatures use plain reference types.
- **Culture.** Numeric parsing always passes `CultureInfo.InvariantCulture`; the suite also builds with `InvariantGlobalization=true`.
- **Hashes.** Always lower-case hex SHA-256 via `Convert.ToHexStringLower`.
