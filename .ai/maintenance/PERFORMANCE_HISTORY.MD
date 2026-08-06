# Performance History

## 2026-08-06 (baseline after analyzer setup)

Environment: Windows 11, .NET SDK 10.0.302, Debug configuration.

### Build time
- Command: `Measure-Command { dotnet build src\MeatyMod.sln -v q }`
- Incremental (up-to-date): **3.1 s** (3150 ms)
- Forced rebuild (`-t:Rebuild`, analyzers re-run): **5.7 s** (5659 ms; MSBuild reports 5.08 s)
- Result: success, 0 errors, 240 warnings (AnalysisMode All baseline).

### Test time
- Command: `Measure-Command { dotnet test src\MeatyMod.Tests\MeatyMod.Tests.csproj }`
- Wall time: **11.3 s** (11322 ms) — includes incremental build of dependencies.
- Result: **53 passed, 0 failed, 0 skipped** (test execution duration: 1 s).

### Game asset verification
- Command: `dotnet run --project src\MeatyMod.Cli --no-build -- verify "game\Blood and Bacon"`
- Wall time: **1.4 s** (1427 ms) — includes `dotnet run` startup.
- Result: **Valid: 1860, Invalid: 0, Total: 1860** (1860 xnb files, all verified).

### Prior baseline (from old report)
- 1.0.0: startup <100 ms, manifest build <500 ms (1396 assets). Baseline recorded.
