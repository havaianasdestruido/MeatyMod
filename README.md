# MeatyMod — mod suite for Blood & Bacon (XNA 4.0, Steam 434570)

MeatyMod is a C# .NET mod suite for Blood & Bacon. It packs content mods, injects .NET mod DLLs into the game executable via Mono.Cecil IL patching, parses the game's XNB/TXT/RAW asset formats, and verifies/restores installs. Ships two example mods (QuackMenu, Oink), both targeting .NET 4.0 / XNA 4.0.

## Projects

| Project | Purpose |
| --- | --- |
| MeatyMod.Cli | `meatymod` CLI (pack/install/inject/restore/manifest/verify/parse) |
| MeatyMod.Core | ModManifest, ModManifestLoader, BackupManager, VersionInfo 0.1.0, FileSizeGuard |
| MeatyMod.Formats | XnbReader + TxtReader, CameraTrackParser, DialogueParser |
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

- `meatymod pack <mod-dir>` — pack a mod directory into a zip.
- `meatymod install <mod-zip> <game-path>` — install a mod zip into a game folder.
- `meatymod inject <game-exe> <mod-dll> [output-exe] [--entry <TypeName>]` — IL-patch a mod into the exe.
- `meatymod restore <patched-exe>` — restore a patched exe from backup.
- `meatymod manifest <game-content-dir> [out.json]` — build an asset manifest.
- `meatymod verify <xnb-file-or-dir>` — validate XNB files.
- `meatymod parse <txt-file>` — parse a TXT asset (day file, camera track, or dialogue).

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
dotnet run --project src\MeatyMod.Cli --no-build -- inject "game\Blood and Bacon\BloodandBacon.exe" mods\Oink\Oink.dll
```

Restore the original exe:

```
dotnet run --project src\MeatyMod.Cli --no-build -- restore "game\Blood and Bacon\BloodandBacon.exe"
```

## Notes / Limits

- One mod injectable at a time (IL ctor patch).
- `game\` is read-only and gitignored.
- XNA 4.0 only; .NET 4.0 targets.
