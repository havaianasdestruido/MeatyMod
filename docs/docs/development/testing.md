---
id: testing
title: Testing
sidebar_label: Testing
description: The xUnit suite — what each file covers and how to run it.
---

# Testing

```bat
dotnet test src\MeatyMod.Tests\MeatyMod.Tests.csproj
```

79 `[Fact]` tests across 15 files. No `[Theory]`/`[InlineData]` — every case is an explicit fact, which keeps failure names self-describing.

## Suite layout

| File | Test class(es) | Facts | Covers |
| --- | --- | --- | --- |
| `BackupManagerTests.cs` | `BackupManagerTests` | 3 | round-trip backup/restore, directory creation, rejection of paths outside the backup root |
| `ChecksumTests.cs` | `ChecksumUtilTests` | 2 | `Sha256File` / `Sha256` against known vectors |
| `CoreTests.cs` | `FileSizeGuardTests`, `ModManifestLoaderTests` | 5 | the 100 MiB guard (including the non-positive `maxBytes` fallback) and manifest loading |
| `ManifestValidationTests.cs` | `ManifestValidationTests` | 14 | every `ModManifest.Validate` rule and message |
| `PackInstallTests.cs` | `PackInstallTests` | 5 | `pack` filtering and archive shape, `install` extraction and backups |
| `InstallChecksumTests.cs` | `InstallChecksumTests` | 4 | checksum verification, mismatch abort, missing entry, malformed line |
| `InjectorTests.cs` | `InjectorTests` | 4 | the happy path of `AssemblyInjector.Patch` |
| `InjectEdgeTests.cs` | `InjectEdgeTests` | 13 | multi-mod ordering, `--entry` resolution, in-place patching, backups, every failure message |
| `VerifyCommandTests.cs` | `VerifyCommandTests` | 2 | `verify` exit codes for files and directories |
| `XnbReaderTests.cs` | `XnbReaderTests` | 3 | header peek and the `Not XNB file.` path |
| `XnbContentReaderTests.cs` | `XnbContentReaderTests` | 5 | magic, version, length and compression branches |
| `LzxFixtureTests.cs` | `LzxFixtureTests` | 2 | LZX decompression against the real `Fixtures\darkFog3_0.xnb` |
| `TxtReaderTests.cs` | `TxtReaderTests`, `CameraTrackParserTests`, `DialogueParserTests` | 9 | trimming, blank-line removal, typed accessors, keyframe grouping, dialogue lines |
| `RawReaderTests.cs` | `RawReaderTests` | 5 | reading, sample values, dimension inference |
| `RawReaderEdgeTests.cs` | `RawReaderEdgeTests` | 3 | too-short files, odd lengths, invalid dimensions |
| `TestAssembly.cs` | — | — | `[assembly: CollectionBehavior(DisableTestParallelization = true)]` |

:::note Parallelisation is off on purpose
Many tests create temp directories, patch assemblies and set the process working directory. Running them in parallel would make `BackupManager`'s CWD-relative mapping and the injector's in-place writes flaky. Keep the attribute.
:::

## How the injector is tested without the game

`InjectorTests` and `InjectEdgeTests` build **synthetic assemblies at runtime** with Mono.Cecil: a fake host exposing `Blood.myGame` with an instance constructor, and fake mod DLLs exposing static `Inject` methods. The tests then patch the fake host and inspect the resulting IL.

That means the whole injection path is covered in CI without shipping a copy of Blood & Bacon — and the failure messages (`Blood.myGame type not found in game assembly.`, `Mod entry type not found: …`, and so on) are asserted as strings, so changing the wording breaks tests deliberately.

## Fixture

`src\MeatyMod.Tests\Fixtures\darkFog3_0.xnb` is a real compressed asset from the game, copied to the output directory via:

```xml
<Content Include="Fixtures\darkFog3_0.xnb">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```

It exists so the LZX decoder is validated against ground truth rather than synthetic data.

## Running a subset

```bat
dotnet test src\MeatyMod.Tests\MeatyMod.Tests.csproj --filter "FullyQualifiedName~InjectEdgeTests"
dotnet test src\MeatyMod.Tests\MeatyMod.Tests.csproj --filter "FullyQualifiedName~Lzx"
dotnet test src\MeatyMod.Tests\MeatyMod.Tests.csproj -v n
```

## Coverage

`coverlet.collector` is referenced, so:

```bat
dotnet test src\MeatyMod.Tests\MeatyMod.Tests.csproj --collect:"XPlat Code Coverage"
```

writes a Cobertura report under `TestResults\<guid>\`.

## Beyond unit tests

| Layer | Tool |
| --- | --- |
| mod code actually executes | [`ModHarness`](../tools/modharness.md) — 18 headless checks |
| DLL loaded in the live game | [`memscan`](../tools/memscan.md) |
| game content intact after install | `meatymod verify` + [`tools\smoke.ps1`](../tools/overview.md#smoke-test) |

The project also keeps QA registries under `.ai/qa/` (functional tests, static analysis, performance, release checklist) — the paper trail behind the automated suite.

## Writing a new test

- One behaviour per fact; name it `Method_Scenario_Expectation`.
- Use a temp directory per test and clean it up; never touch `game\`.
- Assert exit codes **and** on-disk effects for command tests:

  ```csharp
  var exit = new PackCommand().Run(new[] { modDir, zipPath });
  Assert.Equal(0, exit);
  Assert.True(File.Exists(zipPath));
  ```

- Assert the exact error message when documenting a failure mode — the docs quote them.
