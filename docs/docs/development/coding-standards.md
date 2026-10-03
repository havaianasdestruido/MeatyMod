---
id: coding-standards
title: Coding standards
sidebar_label: Coding standards
description: The .editorconfig rules, naming conventions and house style the repository enforces.
---

# Coding standards

Style is enforced by `.editorconfig` at the repository root with `EnforceCodeStyleInBuild=true`, so violations show up as build warnings rather than review comments.

## Files and line endings

| Rule | Applies to |
| --- | --- |
| UTF-8, final newline, no trailing whitespace | everything |
| **CRLF** | `.ps1`, `.psm1`, `.md`, `.sln`, `.slnx`, `.csproj`, `.props`, `.targets`, `.json`, `.xml`, `.yml`, `.yaml`, `.cs` |
| 4-space indentation | `.cs` |

This is a Windows-native repository; do not "fix" the line endings.

## C# style

Selected rules, all from `.editorconfig`:

| Preference | Setting |
| --- | --- |
| `var` for built-in types, apparent types and elsewhere | `true:warning` |
| file-scoped namespaces | `true:warning` |
| braces always | `true:warning` |
| `readonly` fields where possible | `true:warning` |
| accessibility modifiers on non-interface members | `true:warning` |
| `using` directives outside the namespace, `System` first, no group separation | suggestion |
| switch expressions, pattern matching, simplified interpolation, collection expressions | suggestion |
| expression-bodied members | `true` for properties/indexers/accessors/lambdas, `when_on_single_line` for methods, `false` for constructors |
| parentheses in binary operators | `always_for_clarity` |
| `this.` qualification | discouraged |

:::note Older files predate the namespace rule
Some files still use block-scoped namespaces (`MeatyMod.Cli.Program`, `XnbReader`, `FileSizeGuard`, `VersionInfo`, several commands). That is historical. New code uses file-scoped namespaces; converting an old file is a welcome, self-contained change.
:::

## Naming

| Symbol | Convention | Severity |
| --- | --- | --- |
| private fields (instance and static) | `_camelCase` | suggestion |
| locals and parameters | `camelCase` | warning |
| public / protected members, types, constants, enums | `PascalCase` | warning |
| interfaces | `IPascalCase` | warning |
| type parameters | `TPascalCase` | suggestion |
| static readonly fields | `PascalCase` | suggestion |

The mod projects (`net40`, `LangVersion 7.3`) follow the same naming but cannot use the newer language features. `LzxDecoder` deliberately keeps the lower-case/underscore names of the reference implementation it was ported from, so it stays diffable against its source.

## Analyzers

`Directory.Build.props` sets `AnalysisLevel=latest` and `AnalysisMode=All` — every .NET analyzer rule is enabled, with `TreatWarningsAsErrors=false`.

Suppress narrowly and **always** say why:

```csharp
#pragma warning disable CA1515, CA1031 // Public ICommand classes and catch-all Run exit paths are the established CLI convention.
```

```csharp
#pragma warning disable CA5389 // entry.FullName is containment-validated against contentRoot above.
using (var entryStream = entry.Open())
…
#pragma warning restore CA5389
```

A suppression without a justification comment is a review failure. Prefer the narrowest scope that works: a `restore` immediately after the risky block beats a file-level disable.

## Architectural conventions

- **Libraries never write to the console.** Only `MeatyMod.Cli` and the mods produce output.
- **Commands never throw.** `ICommand.Run` catches, prints to `stderr`, returns `1`.
- **Static-first libraries.** Add a class only when there is genuine state (`BackupManager`, `TxtDocument`).
- **No new package references in the leaf libraries.** `Core`, `Formats`, `Assets` and `Verifier` must stay dependency-free; `Mono.Cecil` belongs to `Injector` alone.
- **Invariant culture everywhere.** `InvariantGlobalization=true` is set; always pass `CultureInfo.InvariantCulture` to numeric parsing.
- **Mods fail soft.** Reflection helpers log and return `null`/no-op; they never throw inside the render loop.
- **Error messages are API.** They are asserted in tests and quoted in these docs — changing one is a deliberate, documented change.

## Formatting before a commit

```bat
dotnet format src\MeatyMod.sln
dotnet build src\MeatyMod.sln
dotnet test src\MeatyMod.Tests\MeatyMod.Tests.csproj
```

`dotnet format` reads the same `.editorconfig`, so it will not fight the build.

## Documentation style

The docs in `docs/` follow the same spirit as the code:

- sentence case headings;
- state what the code **does**, not what it should do — if behaviour is surprising, document the surprise in a `:::caution`;
- quote real output and real error strings;
- Windows paths with backslashes in command examples;
- every new CLI command gets a page under `docs/docs/cli/` and an entry in `sidebars.ts`.
