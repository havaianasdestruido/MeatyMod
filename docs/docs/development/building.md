---
id: building
title: Building
sidebar_label: Building
description: Build configurations, output paths and what all.bat actually does.
---

# Building

## One command

```bat
all.bat
```

Release-builds, in order: `src\MeatyMod.sln`, `tools\modharness`, `tools\memscan`, `mods\Oink`, `mods\QuackMenu`. It stops at the first failure with `[all] <stage> build FAILED.` and exit code `1`.

## Per-target

```bat
dotnet build src\MeatyMod.sln                                   :: whole suite (Debug)
dotnet build src\MeatyMod.sln -c Release
dotnet build src\MeatyMod.Cli\MeatyMod.Cli.csproj               :: just the CLI
dotnet build mods\Oink\src\Oink\Oink.csproj -c Release          :: one mod
dotnet build tools\modharness\ModHarness.csproj -c Release
dotnet build tools\memscan\MemScan.csproj -c Release
```

## Shared properties

`Directory.Build.props` applies to every project under the repository root:

```xml
<TargetFramework>net10.0</TargetFramework>
<ImplicitUsings>enable</ImplicitUsings>
<Nullable>enable</Nullable>
<LangVersion>latest</LangVersion>
<AnalysisLevel>latest</AnalysisLevel>
<AnalysisMode>All</AnalysisMode>
<TreatWarningsAsErrors>false</TreatWarningsAsErrors>
<EnableNETAnalyzers>true</EnableNETAnalyzers>
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
<InvariantGlobalization>true</InvariantGlobalization>
<NuGetAudit>false</NuGetAudit>
```

`AnalysisMode=All` means **every** .NET analyzer rule is on. Warnings are not errors, but the build is noisy by design — fix or justify them rather than ignoring them. Justify in place:

```csharp
#pragma warning disable CA1031 // catch-all Run exit paths are the established CLI convention
```

## Per-project overrides

| Project | Override | Why |
| --- | --- | --- |
| `MeatyMod.Cli` | `OutputType=Exe`, `AssemblyName=meatymod`, `ImplicitUsings=disable`, `Nullable=disable` | ships as `meatymod.exe`; explicit usings |
| `MeatyMod.Injector` | `ImplicitUsings=disable`, `Nullable=disable`, `PackageReference Mono.Cecil 0.11.6` | Cecil APIs predate nullable annotations |
| `MeatyMod.Tests` | `IsPackable=false`, xUnit + coverlet + Cecil, `NoWarn` for test-only analyzer noise, copies `Fixtures\darkFog3_0.xnb` | |
| `mods\*` | `TargetFramework=net40`, `LangVersion=7.3`, XNA `HintPath` references | the game is .NET 4.0 / XNA 4.0 |
| `tools\modharness` | `net10.0-windows`, `PlatformTarget=x86`, `UseWindowsForms=true` | XNA assemblies are x86-only |
| `tools\memscan` | `net10.0`, `ImplicitUsings=disable` | Win32 P/Invoke |

## Output paths

| Artifact | Path |
| --- | --- |
| `meatymod.exe` | `src\MeatyMod.Cli\bin\{Debug,Release}\net10.0\meatymod.exe` |
| `Oink.dll` | `mods\Oink\src\Oink\bin\Release\net40\Oink.dll` |
| `QuackMenu.dll` | `mods\QuackMenu\src\QuackMenu\bin\Release\net40\QuackMenu.dll` |
| `ModHarness.exe` | `tools\modharness\bin\Release\net10.0-windows\ModHarness.exe` |
| `memscan.exe` | `tools\memscan\bin\Release\net10.0\memscan.exe` |
| release zip | `dist\meatymod-<version>.zip` |

`bin\`, `obj\` and `dist\` are git-ignored.

## Dependencies

| Package | Version | Used by | Redistributed |
| --- | --- | --- | --- |
| `Mono.Cecil` | 0.11.6 | `MeatyMod.Injector` (and the tests) | ✅ in `lib\` of the release zip |
| `xunit` | 2.9.3 | tests | ❌ |
| `xunit.runner.visualstudio` | 3.1.4 | tests | ❌ |
| `Microsoft.NET.Test.Sdk` | 17.14.1 | tests | ❌ |
| `coverlet.collector` | 6.0.4 | tests | ❌ |

`Core`, `Formats`, `Assets` and `Verifier` have **no** package references — keep it that way. See [Third-party notices](../reference/third-party.md).

## Clean

```bat
dotnet clean src\MeatyMod.sln
for /d /r %d in (bin obj) do @if exist "%d" rd /s /q "%d"
```

## Releasing

```powershell
powershell -ExecutionPolicy Bypass -File tools\release.ps1
```

Bump `MeatyMod.Core.VersionInfo.Version` **first** — the script parses that constant to name the archive. Details in [Tools & scripts](../tools/overview.md#release).

## Building the documentation

The two documentation sites are independent of the .NET build:

```bash
cd docs && npm install && npm start     # Docusaurus at /docs
cd site && bundle exec jekyll serve     # the marketing site
```

See [Documentation site](./documentation-site.md).
