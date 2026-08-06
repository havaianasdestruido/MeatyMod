# Static Analysis Report

- Date: 2026-08-06
- Toolchain: .NET SDK 10.0.302, Windows PowerShell 5.1
- Baseline command: `dotnet build src\MeatyMod.sln` (full rebuild for analyzer pass)

## Analyzer Setup Added

Config files owned by this task:

1. `Directory.Build.props` (repo root) — added to existing `PropertyGroup`:
   - `LangVersion` latest
   - `AnalysisLevel` latest
   - `AnalysisMode` All
   - `TreatWarningsAsErrors` false (intentionally NOT enabled)
   - `EnableNETAnalyzers` true
   - `EnforceCodeStyleInBuild` true
   - `InvariantGlobalization` true
   - `NuGetAudit` false (restore verified OK — no NuGetAudit block errors)
   - Existing settings (`TargetFramework` net10.0, `ImplicitUsings` enable, `Nullable` enable) kept intact.
2. `.editorconfig` (repo root) — new: charset utf-8, CRLF for `.ps1/.md/.sln/.csproj`, 4-space indent for `.cs`, `insert_final_newline = true`, full `dotnet_style_*` / `csharp_style_*` / naming rules, `dotnet format` compatible.

## Measured Results

Command: `dotnet build src\MeatyMod.sln -t:Rebuild -v m` (forced rebuild so analyzers actually run)

| Metric | Value |
|---|---|
| Build result | SUCCESS — 0 errors |
| Compiler warnings (CS8618) | 2 |
| Analyzer/style warnings | 238 (MSBuild summary: 240) |
| **Total warnings** | **240** |
| Total errors | 0 |
| Wall time (full rebuild) | 5.7 s (MSBuild: 5.08 s) |

Note: incremental `dotnet build` (no-op, up to date) reports 0/0 — analyzers only re-run when compilation is re-invoked. Clean/forced rebuild is the source of truth for counts.

## Per-Project Warning Counts (from solution rebuild)

| Project | Warnings |
|---|---|
| src\MeatyMod.Formats | 138 |
| src\MeatyMod.Cli | 77 |
| src\MeatyMod.Injector | 6 |
| src\MeatyMod.Verifier | 7 |
| src\MeatyMod.Core | 7 |
| src\MeatyMod.Assets | 2 |
| src\MeatyMod.Tests | 1 |

## Prioritized Analyzer Findings (for orchestrator to action later)

Prioritized by count, then by risk. Do NOT fix others' source files — these are owned by parallel agents.

### P1 — bulk, low-risk, high-count (automated fixes fine)
- **IDE0007 (65)** — use `var` instead of explicit type. `dotnet format` auto-fixable.
- **IDE0011 (51)** — add braces to single-line `if`. `dotnet format` auto-fixable.
- **IDE0161 (15)** — convert block-scoped namespaces to file-scoped (rule set to `warning`).

### P1 — naming consistency (review each; some are false positives on de/reflection code)
- **IDE1006 (21)** — naming rule violations, mostly missing `_` prefix on private fields in `MeatyMod.Formats\LzxDecoder.cs`. NOTE: decoder mirrors LZX spec naming; suppress or rename with care.
- **IDE0040 (3)** — missing accessibility modifiers, `LzxDecoder.cs` (internal `BitBuffer`).
- **IDE0017 (2) / IDE0028 (2) / IDE0300 (2)** — object/collection initializer simplifications.

### P2 — correctness / robustness
- **CA1062 (11)** — validate public method params for null (CLI command `Run(string[] args)`).
- **CA1031 (9)** — broad `catch (Exception)` — capture specific types or rethrow.
- **CA1822 (3)** — members can be `static` (`ParseCommand.RunRaw`, `LzxDecoder.MakeDecodeTable`, `ReadHuffSym`).
- **CS8618 (2)** — non-nullable fields `Magic`/`Content` in `XnbContentReader.cs` — pre-existing compiler warning.
- **CA1510 (2)** — use `ArgumentNullException.ThrowIfNull`.
- **CA1852 (2)** — types can be `sealed` (`LzxDecoder`, `BitBuffer`).
- **CA1515 (10)** — consider `internal` (CLI command classes + `ICommand`).

### P3 — low count / quality
- **CA1051 (16)** — visible instance fields (`CameraTrackParser.cs` — review, may be data holder).
- **CA1815 (2)** — `CameraKeyframe` should override `Equals`/`==`.
- **CA1002 (1) / CA2227 (1)** — `ModManifest.Replaces`: use `Collection<T>` + read-only property.
- **CA1869 (1)** — cache `JsonSerializerOptions` (`ManifestCommand.cs:35`).
- **CA1847 (1)** — use `string.Contains(char)`.
- **CA2022 (1)** — avoid inexact `Stream.Read` (`LzxDecoder.cs:333`).
- **CA2201 (1)** — don't raise `System.Exception` (`LzxDecoder.cs:18`).
- **IDE0063 (1)** — simplify `using` (test file).
- **IDE0042 (1) / IDE0044 (1) / IDE0290 (1) / IDE0004 (1) / IDE0078 (2) / IDE0010 (2)** — misc style (naming/simplification).

## Notes
- Parallel agents own the `.cs` files; this report only catalogs findings — it does not modify their code.
- Before this task's config landed, a transient build failure (4× CS0246/CS0103 in `XnbCommand.cs`) was observed mid-edit by another agent; it was resolved by that agent before final measurement. Current build: 0 errors.
- Warnings flood (240) is accepted per task scope; `TreatWarningsAsErrors` remains false so parallel work is not blocked.

## Verdict
PASS — builds clean (0 errors), analyzer baseline established and measurable. 240 warnings tracked for downstream remediation.
