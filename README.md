# MeatyMod

MeatyMod is a C# .NET mod suite for Blood & Bacon. It packs content mods, injects .NET mod DLLs into the game executable via Mono.Cecil IL patching, parses the game's XNB/TXT/RAW asset formats, and verifies/restores installs. Ships two example mods (QuackMenu, Oink), both targeting .NET 4.0 / XNA 4.0.

## TL:DR

This is a WIP modding framework that injects custom mods .DLL into the games executable, then it makes an Frankenstein-ish binary that call the custom mod code you selected.

**You can revert the game binary to the non-modded version with `meatymod restore`.**

As it sounds, only add mods YOU TRUST.

I will not add a whole ass script for detecting if it will execute MEMZ or any other BS.

**PLEASE** check your mods.

## Projects

| Project | Purpose |
| --- | --- |
| MeatyMod.Cli | `meatymod` CLI (pack/install/inject/restore/manifest/verify/parse/xnb/checksum) |
| MeatyMod.Core | ModManifest, ModManifestLoader, BackupManager, ChecksumUtil, VersionInfo 1.0.0, FileSizeGuard |
| MeatyMod.Formats | XnbReader (XNB decompression), TxtReader, RawReader, CameraTrackParser, DialogueParser |
| MeatyMod.Assets | AssetManifestBuilder |
| MeatyMod.Verifier | AssetValidator (ValidateXnb, ValidateDirectory) |
| MeatyMod.Injector | Mono.Cecil injection, auto-detects mod entry type |
| MeatyMod.Tests | xUnit test suite |

## Mods

| Mod | What it does | Toggle |
| --- | --- | --- |
| QuackMenu | creative mode + boss menu | F1 |
| Oink | pig skin + speed | O |

Install a mod by packing it to a zip and injecting the DLL into the game exe (`game\Blood and Bacon\BloodandBacon.exe`). Each mod carries its own `lib\xna\`.

## CLI

- `meatymod pack <mod-dir>` — pack a mod directory into a zip (writes `checksums.txt` inside).
- `meatymod install <mod-zip> <game-path>` — install a mod zip into a game folder.
- `meatymod inject <game-exe> [--mod <dll> [--entry <TypeName>]]... [output-exe]` — IL-patch one or more mods into the exe.
- `meatymod restore <patched-exe>` — restore a patched exe from backup.
- `meatymod manifest <game-content-dir> [out.json]` — build an asset manifest.
- `meatymod verify <xnb-file-or-dir>` — validate XNB files.
- `meatymod parse <file>` — parse a TXT asset (day file, camera track, or dialogue) or a `.raw` heightmap.
- `meatymod xnb <xnb-file-or-dir>` — dump XNB header info (platform, version, flags, compressed/decompressed sizes).
- `meatymod checksum <file-or-dir>` — print SHA-256 checksums for a file or every file under a directory.

`pack` embeds a `checksums.txt` (relative path + SHA-256 per file) in every mod zip; `checksum` reproduces that list for any file or directory.

## Build

```
dotnet build src\MeatyMod.sln
```

Or per-project:

```
dotnet build src\MeatyMod.Cli\MeatyMod.Cli.csproj
```

## Tests

```
dotnet test src\MeatyMod.Tests\MeatyMod.Tests.csproj
```

## Examples

Build the CLI, then pack and inject the example mods:

```
dotnet build src\MeatyMod.Cli\MeatyMod.Cli.csproj
dotnet run --project src\MeatyMod.Cli --no-build -- pack mods\QuackMenu
dotnet run --project src\MeatyMod.Cli --no-build -- pack mods\Oink
dotnet run --project src\MeatyMod.Cli --no-build -- inject "game\Blood and Bacon\BloodandBacon.exe" --mod mods\QuackMenu\src\QuackMenu\bin\Debug\net40\QuackMenu.dll --mod mods\Oink\src\Oink\bin\Debug\net40\Oink.dll
```

Restore the original exe:

```
dotnet run --project src\MeatyMod.Cli --no-build -- restore "game\Blood and Bacon\BloodandBacon.exe"
```

## Release

- `tools\release.ps1` — build the CLI, pack the example mods, and assemble the release artifact (dist zip) with checksums.
- `THIRD_PARTY_NOTICES.md` — third-party notices for bundled dependencies.
- `tools\smoke.ps1` — game smoke suite for post-install verification.

## Notes / Limits

- Multi-mod injection: pass repeated `--mod <dll>` to `inject` to patch several mods into one exe.
- `game\` is read-only and gitignored.
- XNA 4.0 only; .NET 4.0 targets.
