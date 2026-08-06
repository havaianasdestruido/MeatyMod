# Code Review Report

- Date: 2026-08-06
- Scope: All `src\MeatyMod.*\**\*.cs`, `*.csproj`, `Directory.Build.props`, `src\MeatyMod.sln`, `tools\*.ps1`, plus both mods (`mods\Oink`, `mods\QuackMenu`) and `manifest.schema.json`.
- Method: Full read of every source file; findings cite `file:line` (spot-checked against the working tree).
- Caveat: Several files are mid-edit by parallel agents (untracked or modified vs HEAD): `XnbContentReader.cs`, `LzxDecoder.cs`, `RawReader.cs`, `ChecksumUtil.cs`, `ChecksumCommand.cs`, `XnbCommand.cs`, the new test files, `InjectCommand.cs`, `InstallCommand.cs`, `PackCommand.cs`, `ModManifest.cs`, `ModManifestLoader.cs`, `BackupManager.cs`, `AssemblyInjector.cs`. Findings below reflect the tree as reviewed; the two "blocking/unwired" items (F01) are explicitly flagged as possibly in-flight.

## Findings

### High

| Severity | File:Line | Issue | Recommendation |
| --- | --- | --- | --- |
| High | `src/MeatyMod.Cli/Program.cs:11-20` | `ChecksumCommand` and `XnbCommand` are never registered in the dispatch list, yet `README.md:35-36` documents them as available. The commands compile but are unreachable. (May be in-flight — `Program.cs` is unchanged vs HEAD.) | Register both in `Program.cs:11-20` (or drop the README claims). Gate feature completion on this landing. |
| High | `src/MeatyMod.Formats/XnbReader.cs:12-17`, `src/MeatyMod.Formats/XnbContentReader.cs:21-27`, `src/MeatyMod.Verifier/AssetValidator.cs:19` | Three independent XNB readers with three **divergent** validity rules: magic-only (`XnbReader.ReadHeader`), magic + version==5 (`XnbContentReader.Read`), magic + platform 0x77 + version 5 (`AssetValidator.ValidateXnb`). Same format knowledge duplicated across 3 projects; a file can pass one check and fail another. | Consolidate into a single reader in `MeatyMod.Formats` (drop `XnbHeader`/`XnbReader` or make it delegate). Have `AssetValidator` and `XnbCommand` call it; align the platform/version rules in one place. |
| High | `mods/Oink/src/Oink/OinkEntry.cs:31-34` | Config `enabled=false` has no effect: `_enabled` defaults `true` and the `if (_screenManager != null && ParseBool(_config.Enabled, true))` has no `else`, so the flag is never set false from config. Oink cannot be disabled by config (only by the runtime O toggle). | Add `else { _enabled = false; }` (and decide the `_screenManager == null` case explicitly). |

### Medium

| Severity | File:Line | Issue | Recommendation |
| --- | --- | --- | --- |
| Medium | `src/MeatyMod.Cli/Commands/XnbCommand.cs:85-86` | `SweepDirectory` prints `Failed: N` but returns `0` when failures > 0; `VerifyCommand.cs:37` returns `1` for the same condition. Inconsistent exit semantics break scripted checks. | Return non-zero when `failed > 0`. |
| Medium | `src/MeatyMod.Cli/Commands/PackCommand.cs:56-63` | `checksums.txt` is written into every zip, but `InstallCommand.cs:41-83` never reads or verifies it — the integrity feature is write-only. | Verify `checksums.txt` during install and report/abort on mismatch (reuse `ChecksumUtil`). |
| Medium | `src/MeatyMod.Cli/Commands/PackCommand.cs:37-42` | If the user packs the current working directory (or `modDir == CWD`), the just-created `mod.zip` is enumerated into itself (`Directory.EnumerateFiles` after `FileMode.Create`), risking self-inclusion or a sharing violation. | Exclude the output zip path (and any `.zip`) from enumeration. |
| Medium | `src/MeatyMod.Cli/Commands/InjectCommand.cs:26-102` | Hand-rolled dual-mode argument parser (legacy positional branch at :36-52 vs `--mod`/`--entry` state machine at :53-102) with `pendingEntry`/`current` bookkeeping. Easy to mis-bind an `--entry` to the wrong `--mod`; two codepaths for the same semantics. | Use a small arg-parsing loop that associates `--entry` with the preceding `--mod` directly; delete the legacy branch or route it through the same code. |
| Medium | `src/MeatyMod.Cli/Commands/InjectCommand.cs:137` | Mod config discovery via hardcoded `..\..\..\..\..\config.txt` is tied to a specific bin-folder depth; breaks if layout changes. | Locate `config.txt` by walking up to the mod root or read it from the mod dir argument. |
| Medium | `src/MeatyMod.Cli/Commands/ParseCommand.cs:35` | File-type heuristic `doc.GetString(0).Contains(".") && doc.Count % 6 == 0` misclassifies any 6-line file whose first line contains a dot as a camera track (e.g. a short dialogue). | Detect by extension/structure or explicit `--kind` flag; treat camera-only as explicit. |
| Medium | `src/MeatyMod.Core/BackupManager.cs:18,39` | Backup paths are anchored to `Directory.GetCurrentDirectory()`, so results depend on process CWD. Also the whole class is dead code (no caller anywhere); three backup schemes coexist (`.backup` in `InjectCommand.cs:117`/`RestoreCommand.cs:19`, `Backups\MeatyMod` in `InstallCommand.cs:36`, `BackupManager`). | Anchor to an explicit base path (the game root), and either adopt `BackupManager` in `RestoreCommand`/`InstallCommand` or delete it. |
| Medium | `src/MeatyMod.Core/ModManifestLoader.cs:22-26` | `Load` swallows *all* exceptions (`catch { return null; }`), hiding IO/JSON failures as "missing manifest". `LoadWithValidation` (:29-52) already provides the good behavior; two parallel APIs. | Drop the silent `Load` or have it delegate to `LoadWithValidation`; surface the error string. |
| Medium | `src/MeatyMod.Formats/RawReader.cs:60-70` | `TryGuessDimensions` returns the **first** size in `{512,1024,2048,4096}` that divides the byte count, so a 1024×2048 heightmap (2097152 samples) is guessed as 512×4096. Ambiguity never resolved. | Prefer the factorization closest to a square (largest `min(w,h)`), and/or accept an explicit `--width/--height`. |
| Medium | `src/MeatyMod.Formats/LzxDecoder.cs:358-374` | Intel E8 fixup loop is a no-op: after `outData.Write(...)` at :351, `outData.Position == outLen > dataend (outLen-10)`, so the loop never runs; and if it did, it only reads bytes — it never rewrites the relocated call offsets. Dead/misleading code; a stream with the intel bit set would yield corrupt output silently. | Remove the block, or implement a real E8 fixup pass that rewrites into `outData`/window and add a test for it. |
| Medium | `src/MeatyMod.Formats/XnbContentReader.cs:61` | `new MemoryStream(decompressedSize)` allocates from an untrusted 4-byte header with no upper bound — a crafted header can trigger a huge allocation (DoS). | Cap `decompressedSize` (e.g. against a sane max per asset) before allocating. |
| Medium | `src/MeatyMod.Verifier/AssetValidator.cs:40` | `ValidateDirectory` catches only `IOException`; an `UnauthorizedAccessException` (not an `IOException`) escapes and aborts the whole directory sweep. Validation is also header-only (5 bytes, :19), so content corruption passes. | Catch `Exception`-level per-file errors (log, count invalid, continue); extend validation to reuse the Formats reader for a deeper check. |
| Medium | `src/MeatyMod.Assets/AssetManifestBuilder.cs:14-15` | Manifest is keyed by bare filename (`Path.GetFileNameWithoutExtension`); two `.xnb` with the same name in different subdirs silently overwrite each other (last wins). | Key by relative path, or detect and report collisions instead of overwriting. |
| Medium | `src/MeatyMod.Injector/AssemblyInjector.cs:82-99` | Entry-type resolution hardcodes legacy `"QuackMenu.QuackMenuEntry"` (:89) and otherwise scores types by `Name.EndsWith("Entry")` (:97) — arbitrary for a DLL with several `Inject(Game)` types. Multi-mod injection makes ambiguous selection likely. | Require explicit `--entry` when more than one candidate exists; error out instead of guessing. |
| Medium | `mods/QuackMenu/src/QuackMenu/BossCatalog.cs:23` | `BossCatalog.Enabled(config)` is never called; `BossMenuScreen.cs:28` always uses `BossCatalog.All`, so the parsed `bosses=`/`bossweights=` config (`QuackConfig.cs:13-14,48-49`) is dead. | Wire `Enabled(config)` into `BossMenuScreen` (or delete the config keys). |
| Medium | `src/MeatyMod.Tests/XnbContentReaderTests.cs:11` | Test hardcodes `C:\Users\mcmco\Desktop\MeatyMod\game\...` — fails on any other machine / CI. | Skip gracefully (or `Assert.Skip`) when the game data is absent; never assert on an absolute user path. |
| Medium | `src/MeatyMod.Tests/MeatyMod.Tests.csproj:22-23` | Tests reference only `MeatyMod.Formats` and `MeatyMod.Core`. `Verifier`, `Assets`, `Injector`, and all `Cli` commands have **zero** coverage, and `LzxDecoder` has no deterministic positive decompression test (only the real-game test exercises it). | Add projects to the test csproj; add a fixture-based LZX round-trip test and tests for `AssetValidator.ValidateDirectory`, `AssetManifestBuilder`, `AssemblyInjector.Patch`. |

### Low

| Severity | File:Line | Issue | Recommendation |
| --- | --- | --- | --- |
| Low | `src/MeatyMod.Cli/Commands/PackCommand.cs:18` | Usage string says `meaty pack` while every other command says `meatymod`. | Unify to `meatymod`. |
| Low | `src/MeatyMod.Cli/Commands/VerifyCommand.cs:37` | Empty directory yields `Total 0 == Valid 0` → exit 0; a verify on a path with no `.xnb` passes silently. | Return non-zero (or at least warn) when `Total == 0`. |
| Low | `src/MeatyMod.Cli/Commands/ParseCommand.cs:77-82` | Unknown raw dimensions silently default to 2048×2048, then `RawReader.Read` throws "too short" for legit smaller files; no explicit error message. | Surface the guess failure and let the user pass dimensions. |
| Low | `src/MeatyMod.Formats/TxtReader.cs:21-40` | `GetString`/`GetInt`/`GetFloat` return empty/`0` for out-of-range or unparseable lines — corrupt data reads as zero silently. | Provide `Try*` variants as the primary API (they exist at :42-54) and make callers use them. |
| Low | `src/MeatyMod.Formats/TxtReader.cs:62-68,71-79` + `src/MeatyMod.Formats/DialogueParser.cs:12-17` | The trim/skip-nonempty line loop is duplicated three times. | Extract one shared `ReadNonEmptyLines(path/stream)` helper. |
| Low | `src/MeatyMod.Formats/CameraTrackParser.cs:38-42` | Trailing partial group (fewer than 6 floats) is silently dropped at EOF (test `TxtReaderTests.cs:84` codifies it). | At least log/warn on dropped trailing data. |
| Low | `src/MeatyMod.Formats/LzxDecoder.cs:18` | `throw new Exception("Unsupported window")` — bare `Exception`. | Use `ArgumentOutOfRangeException`/`InvalidDataException`. |
| Low | `src/MeatyMod.Formats/LzxDecoder.cs:8-9` | `extra_bits`/`position_base` are mutable `public static` tables (not `readonly`). | Mark `static readonly` (and fold into the state). |
| Low | `src/MeatyMod.Core/FileSizeGuard.cs:14-17` | `maxBytes <= 0` silently resets to the 100 MB default; a caller passing `0` expecting "reject everything" gets 100 MB. | Treat `<= 0` as "reject all" or require explicit opt-in. |
| Low | `src/MeatyMod.Injector/AssemblyInjector.cs:49` | Injection point = *last* `Ret` in the ctor body; if the last `Ret` is nested in a try/finally, insertion is fragile. | Prefer the method-level epilogue ret (or verify single ret). |
| Low | `src/MeatyMod.Injector/AssemblyInjector.cs:78` | In-place `gameModule.Write(outputPath)` when `outputPath == exePath` (the default, `InjectCommand.cs:116`); backup exists but there is no auto-restore if the write fails. Verify Cecil's read-handle behavior on Windows, or write to a temp file and move. | Write to temp + `File.Replace`, or restore the backup automatically on failure. |
| Low | `mods/Oink/src/Oink/OinkEntry.cs:158-168`, `mods/QuackMenu/src/QuackMenu/QuackMenuEntry.cs:169-179` | ~60 lines duplicated between the two mods (`Log`, `ParseKey`, `IsKeyDown`, `FindScreenManager`, edge-trigger key tracking). | Extract a shared `ModSupport`/`MeatyMod.Runtime` helper project. |
| Low | `mods/Oink/src/Oink/OinkHook.cs:10` (`int.MaxValue`) vs `mods/QuackMenu/src/QuackMenu/QuackMenuHook.cs:10` (`int.MinValue`) | When both mods are injected, update ordering (QuackMenu first, Oink last) is implicit and undocumented. | Document the ordering contract in the multi-mod scenario. |
| Low | `README.md:11` | Says "`XnbReader` (XNB decompression)" but the decompression class is `XnbContentReader`; stale naming post-refactor. | Update docs to match the actual reader. |
| Low | `src/MeatyMod.Core/ModManifest.cs:3` (file-scoped) vs `src/MeatyMod.Core/BackupManager.cs:4` (block-scoped) | Inconsistent namespace style across files despite `.editorconfig:78` `file_scoped:warning`. | Run `dotnet format` to normalize to file-scoped. |
| Low | `Directory.Build.props:4-5` vs `src/MeatyMod.Cli/MeatyMod.Cli.csproj:8-9`, `src/MeatyMod.Injector/MeatyMod.Injector.csproj:7-8` | Global `Nullable`/`ImplicitUsings` enabled are overridden to disabled per-project; `AnalysisMode=All` is on but `TreatWarningsAsErrors=false`, so analyzer findings are not enforced. | Pick one nullable mode; consider enabling warnings-as-errors in CI only. |

## Strengths

- Clean project separation (`Core`/`Formats`/`Assets`/`Verifier`/`Injector`/`Cli`/`Tests`) with a single solution; stable `ICommand` contract and centralized usage/dispatch (`Program.cs:40-49`).
- Good defensive posture: `FileSizeGuard` enforced in both pack and install; zip-slip containment properly fixed with `IsInside` (`InstallCommand.cs:94-104`); backup-before-overwrite in install (`InstallCommand.cs:67-71`) and inject (`AssemblyInjector.cs:32`).
- Multi-mod injection landed as a real feature: `AssemblyInjector.Patch` accepts parallel arrays with length validation (`AssemblyInjector.cs:14-23`), IL is emitted in order, and duplicate-DLL-filename deployment is guarded (`InjectCommand.cs:125-134`).
- Manifest validation is well-designed and tested (`ModManifest.Validate`/`LoadWithValidation` + 12 tests), and `manifest.schema.json` matches `ModManifest`.
- Format tests are deterministic and fast (in-memory/temp files), including a known-answer SHA-256 test (`ChecksumTests.cs:11-19`) and a real-game XNB decompression test.
- XNB content reader validates magic, version, and decompressed size; `LzxDecoder` is a complete windowed-LZX port with correct tables.
- `tools/release.ps1` builds, stages, hashes (SHA256SUMS), and zips a release artifact; `tools/smoke.ps1` provides a golden-count game smoke suite.
- Docs and README are being kept current with the parallel work (multi-mod CLI, checksum/xnb commands, version bump to 1.0.0).

## Top 5 Recommended Fixes

1. **Register the new commands** — add `ChecksumCommand` and `XnbCommand` to `Program.cs:11-20` (README already promises them at `README.md:35-36`); verify this lands before closing the feature.
2. **Consolidate XNB reading/validation** into one `MeatyMod.Formats` reader and make `AssetValidator`/`XnbCommand` reuse it (resolve the divergent rules at `XnbReader.cs:12-17`, `XnbContentReader.cs:21-27`, `AssetValidator.cs:19`).
3. **Finish the integrity features** — verify `checksums.txt` on install (`InstallCommand.cs:41-83`) and enforce `ModManifest` validation in `pack` (`PackCommand.cs:40-63`) so they are not write-only.
4. **Fix the two dead-config bugs** — Oink `enabled=false` (`OinkEntry.cs:31-34`) and QuackMenu `Bosses`/`BossWeights` never applied (`BossCatalog.cs:23` vs `BossMenuScreen.cs:28`).
5. **Close the test gaps** — deterministic LZX fixture test (currently only the machine-dependent real-game test exercises decompression, `XnbContentReaderTests.cs:11`), plus coverage for `AssetValidator`, `AssetManifestBuilder`, `AssemblyInjector`, and the CLI wiring.

## Totals

- High: 3
- Medium: 17
- Low: 16
- **Total findings: 36**
