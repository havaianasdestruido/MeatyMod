# Regression Report / Risk Register

- Date: 2026-08-06
- Baseline: `VersionInfo` 1.0.0 (`src/MeatyMod.Core/VersionInfo.cs:5`), commit `06fc8ae`.
- Scope: Features at risk when the current parallel work lands: **multi-mod injection, XNB decompression, checksums, raw parsing** (plus manifest validation and install hardening).
- State note: the following files are untracked/mid-edit and must be present before any of the checks below are meaningful: `XnbContentReader.cs`, `LzxDecoder.cs`, `RawReader.cs`, `ChecksumUtil.cs`, `ChecksumCommand.cs`, `XnbCommand.cs`, `ChecksumTests.cs`, `RawReaderTests.cs`, `XnbContentReaderTests.cs`, `ManifestValidationTests.cs`; modified: `InjectCommand.cs`, `InstallCommand.cs`, `PackCommand.cs`, `ModManifest.cs`, `ModManifestLoader.cs`, `BackupManager.cs`, `AssemblyInjector.cs`, `VersionInfo.cs`.

## Regression Risk Register

| # | Feature | Landing risk / change | Existing guards | Recommended verification |
| --- | --- | --- | --- | --- |
| R1 | **Multi-mod injection** | `InjectCommand.cs:26-102` adds `--mod/--entry` loop; the legacy single-mod positional path (`InjectCommand.cs:36-52`) could silently break. `AssemblyInjector.cs:14-80` now injects in array order; entry auto-detect heuristic changed (`AssemblyInjector.cs:82-99`). In-place write (`outputPath == exePath`, `InjectCommand.cs:116`) and `.backup` convention must stay stable for `restore`. | **None** — `MeatyMod.Tests.csproj:22-23` references only Formats + Core; no test covers Injector or Cli. | Manual: legacy `inject <exe> <dll>` AND multi `inject <exe> --mod a --mod b --entry X`; confirm `.backup` created and `restore` returns the exe byte-identical (compare via `checksum`). |
| R2 | **XNB decompression** | `XnbContentReader.cs` + `LzxDecoder.cs` are new. LZX decode could regress on real compressed assets; the Intel E8 fixup path is a known no-op (`LzxDecoder.cs:358-374`). Header-rule divergence between `XnbReader.cs:12-17`, `XnbContentReader.cs:21-27`, `AssetValidator.cs:19` can disagree on the same file. | `XnbReaderTests.cs` (3), `XnbContentReaderTests.cs` (5 — incl. real-game file at `XnbContentReaderTests.cs:79`). No deterministic LZX fixture. | Once `xnb` is registered: `meatymod xnb game\Blood and Bacon\Content` → compressed > 0, failed == 0. Re-run `XnbContentReaderTests` on a clean machine (drop absolute path first). Add a fixture-based positive LZX test. |
| R3 | **Checksums** | `PackCommand.cs:53-63` embeds `checksums.txt` (format `relpath SPACE sha256`). `InstallCommand.cs` does **not** verify it, so zip-layout or line-format changes are silently invisible. `ChecksumCommand.cs` must reproduce the same `relpath + "  " + hash` format. | `ChecksumTests.cs` (2 — known-answer hash, file stream). No zip-content test. | `meatymod checksum mods\Oink` vs the `checksums.txt` inside a freshly packed zip must match line-for-line. Decide whether install must verify (recommended) and add a test. |
| R4 | **Raw parsing** | `RawReader.cs:60-70` dimension guess returns first divisor (1024×2048 misread as 512×4096). `ParseCommand.cs:75-101` defaults unknown sizes to 2048×2048 and throws "too short" for other valid sizes. | `RawReaderTests.cs` (5 — sequential, LE, too-short, 2048 square, odd length). No non-square test. smoke.ps1 checks byte length only (`tools/smoke.ps1:66-78`). | Add a 1024×2048 `TryGuessDimensions` test (documents the ambiguity). `meatymod parse content\astro\brushes\earth.raw` must print 2048×2048 without error. |
| R5 | **Manifest validation** | `ModManifest.Validate`/`LoadWithValidation` added (`ModManifest.cs:16-51`, `ModManifestLoader.cs:29-52`) but not yet wired into pack/install — behavior unchanged for now, but `manifest.schema.json:19` (`^\d+(\.\d+)*$`) vs `ModManifest.cs:37` (`System.Version.TryParse`) could drift when enforcement lands. | `ManifestValidationTests.cs` (12). | When enforcement is wired: pack a mod with a bad manifest and confirm a clear failure (no silent acceptance). |
| R6 | **Install hardening** | `InstallCommand.cs:44-57,94-104` replaced the loose `StartsWith(contentDir)` check with `IsInside` and per-entry stream copy; risk is over-rejection of legitimate nested `Content\sub\file` entries or behavior change vs `ExtractToFile`. | **None** (CLI not covered by unit tests). | Install a zip with nested subfolders (e.g. `astro\brushes\...`) and confirm all entries land under `Content\` with backups created for overwritten files. |
| R7 | **Version bump** | `VersionInfo.cs:5` 0.1.0 → 1.0.0; `tools/release.ps1:39` regex-parses it. If the version string format changes the release build silently stalls. | None. | `tools\release.ps1` must complete and produce `dist\meatymod-1.0.0.zip` with a non-empty `SHA256SUMS.txt`. |
| R8 | **Verify semantics** | `VerifyCommand.cs:37` treats an empty directory as success (`Total 0 == Valid 0`); if content dir is ever accidentally emptied, verify still "passes". | smoke.ps1 golden count `Valid 1860 / Invalid 0` (`tools/smoke.ps1:37`). | Keep the smoke golden count; consider failing on `Total == 0`. |

## Guarding tests (current suite)

- `XnbReaderTests.cs` — XNB header magic/version/throws.
- `XnbContentReaderTests.cs` — compressed/uncompressed headers, invalid magic/version, real-game decompression.
- `TxtReaderTests.cs` (+ `CameraTrackParserTests`, `DialogueParserTests`) — txt/camera/dialogue parsing.
- `RawReaderTests.cs` — raw read + dimension guess basics.
- `CoreTests.cs` — `FileSizeGuard`, `ModManifestLoader.Load`.
- `ChecksumTests.cs` — SHA-256 known answer + file.
- `ManifestValidationTests.cs` — `ModManifest.Validate` / `LoadWithValidation`.

**Untested (highest regression surface):** most `MeatyMod.Cli` commands, `AssetValidator.ValidateDirectory`, `AssetManifestBuilder`, `BackupManager`, and positive LZX decompression. CLI inject/restore edge + negative cases now partly covered by `InjectEdgeTests` (13).

## Manual Verification Checklist (for the orchestrator)

Run in this order once all parallel edits land:

1. **Build**
   ```
   dotnet build src\MeatyMod.sln
   ```
   (Mods are not in the sln — build them separately when needed.)
2. **Unit tests**
   ```
   dotnet test src\MeatyMod.Tests\MeatyMod.Tests.csproj
   ```
3. **Game smoke**
   ```
   powershell -ExecutionPolicy Bypass -File tools\smoke.ps1
   ```
   Expected golden values: `verify Valid 1860 / Invalid 0` (`tools/smoke.ps1:37-38`), manifest key count 1396 (`:48`), raw `earth.raw`/`earth2.raw` length 8000000 (`:72`), 4 `.wmv` (`:81`).
4. **Pack → install → restore cycle** (use a **staging copy** of `game\Blood and Bacon` — never write under `game\`):
   ```
   dotnet build mods\Oink\src\Oink\Oink.csproj -c Release
   dotnet build mods\QuackMenu\src\QuackMenu\QuackMenu.csproj -c Release
   src\MeatyMod.Cli\bin\Debug\net10.0\meatymod.exe pack mods\Oink
   src\MeatyMod.Cli\bin\Debug\net10.0\meatymod.exe pack mods\QuackMenu
   ```
   a. Inspect each zip: `checksums.txt` present and matches `meatymod checksum <mod-dir>` line-for-line.
   b. `meatymod install mod.zip <staging>` → all entries under `Content\`, no "unsafe entry" skips for legit files, backups written for overwritten files.
   c. Multi-mod: `meatymod inject <staging>\BloodandBacon.exe --mod Oink.dll --mod QuackMenu.dll [--entry <TypeName>]` → expect `Patched`, `<exe>.backup` created, both DLLs deployed.
   d. Legacy single-mod (README form): `meatymod inject <staging>\BloodandBacon.exe <one-dll>` still works.
   e. `meatymod checksum <staging>` before restore; `meatymod restore <staging>\BloodandBacon.exe`; hash of exe must match the pre-inject hash.
5. **Feature spot-checks**
   - `meatymod verify "game\Blood and Bacon"` → `Valid: 1860 Invalid: 0` (unchanged).
   - `meatymod xnb "game\Blood and Bacon\Content"` (once registered) → compressed count > 0, failed == 0.
   - `meatymod parse "game\Blood and Bacon\ABCDE3\2.txt"` and `...\camMove1.txt` (smoke covers) and `...\Content\astro\brushes\earth.raw`.
6. **Runtime (optional, on a staging exe)** — launch the patched game, verify O toggles Oink pig skin/speed and F1 opens the QuackMenu boss list; confirm both mods' hooks coexist (QuackMenu `UpdateOrder = int.MinValue`, Oink `int.MaxValue`).

## Known open items to close before sign-off

- ~~`Program.cs:11-20` must register `ChecksumCommand`/`XnbCommand`~~ — DONE (commit `44653bc`).
- ~~`XnbContentReaderTests.cs:11` absolute path~~ — resolved via relative fallback; real-file test guarded by full sweep.
- R3 (install-time `checksums.txt` verification, `InstallCommand.cs`) — **IN PROGRESS** / still open (being addressed in the same batch). Pack-side format parity already tested.
- R8 (`VerifyCommand.cs:37` empty-dir treated as success, `Valid 0 == Total 0`) — **IN PROGRESS** / still open (being addressed in the same batch). Not yet fixed in code.

## Rigorous-test follow-up (2026-08-06)

Full checklist executed against a staging copy of the game exe (game dir untouched). Five bugs found and fixed:

| # | Bug | Fix |
| --- | --- | --- |
| 1 | `pack` always overwrote `mod.zip` | optional `[output.zip]` arg |
| 2 | `checksums.txt` format diverged from `checksum` command (spacing/path separators) | unified `relpath  sha256`, forward slashes |
| 3 | `pack` included nested `bin/`/`obj/` + dotfiles | excluded at any path depth |
| 4 | in-place inject (`output == exe`) failed file-in-use | write via temp, replace after dispose |
| 5 | **legacy single-mod inject corrupted the mod DLL** (DLL parsed as output path → patched exe written over Oink.dll) | legacy branch rewritten; DLL no longer treated as output |

Verification: 55/55 tests, SMOKE PASS, xnb sweep 1860/1159 compressed/0 failed, multi-mod IL probe shows both Inject calls in order, restore byte-identical.

## Extreme-test follow-up (2026-08-07)

1. **Patched-exe file audit PASS**: Oink AssemblyRef token `[AssemblyRef:0x000e]`; ctor tail exactly `ldarg.0; call Oink.OinkEntry::Inject; ret` (1 Inject MemberReference, 1 ret); deployed `Oink.dll` hash == source; real game exe pristine (hash `9A5D83...E77AD`); restore round-trip byte-exact.
2. **Headless ModHarness** (`tools\modharness`, net10.0-windows x86): 18 PASS / 0 FAIL — loads Oink.dll + XNA via reflection (needs GAC `Microsoft.Xna.Framework.Input.Touch.dll`), constructs ShimGame, Inject executes, hook added with `UpdateOrder = int.MaxValue`, `oink.log` "Oink injected." written, `OinkHook.Update` runs without throw. Closes the "cannot prove mod code runs without launching the game" gap.
3. **Process memory viewer** (`tools\memscan`): module enumeration + heap marker scan verified against a dummy target process.
4. **13 new InjectEdgeTests added** (edge/negative injector + CLI regression), suite now 68/68.
5. **New repo scripts**: `all.bat`, per-mod `build.bat`/`run.bat`.

Open items: R3 (install-time `checksums.txt` verification) and R8 (`VerifyCommand` empty-dir failure) remain **IN PROGRESS / still open** — being addressed in the same batch; not yet resolved in code.

