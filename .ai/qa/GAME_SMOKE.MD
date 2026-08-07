# Game Smoke Test Report

## Purpose
Runs the built `meatymod` CLI against the READ-ONLY game tree (`game\Blood and Bacon`) and asserts baseline facts. The game tree is only read; it is never written.

## How to Run
```
powershell -ExecutionPolicy Bypass -File tools\smoke.ps1
```
Prints `SMOKE PASS` and exits 0, or `SMOKE FAIL <detail>` and exits 1.

## Build / QA Entry Points

| Script | What it does |
| --- | --- |
| `all.bat` | Builds `src\MeatyMod.sln` (Release), `tools\modharness`, `tools\memscan`, then both mods via `mods\Oink\build.bat` and `mods\QuackMenu\build.bat`. All Release. |
| `mods\Oink\build.bat` / `mods\QuackMenu\build.bat` | Mod-only Release build (output `mods\<Mod>\src\<Mod>\bin\Release\net40\<Mod>.dll`). |
| `mods\Oink\run.bat` / `mods\QuackMenu\run.bat` | Build, then `meatymod restore` -> `meatymod inject <dll>` -> launch `BloodandBacon.exe` -> `meatymod restore`. Uses the newest Release/Debug `meatymod.exe`. |

The smoke suite itself is unchanged and remains read-only on the game tree; these scripts are the build/QA entry points that produce the artifacts the smoke suite and manual runs depend on.

## Related QA Tooling

- `tools\modharness` - headless Inject proof harness (x86), supplementary non-smoke check for injection correctness.
- `tools\memscan` - process memory viewer, supplementary non-smoke diagnostic for inspecting a running game process.

Both are built by `all.bat` and are not part of the smoke suite.

## What It Checks
1. **Build** - `dotnet build src\MeatyMod.Cli\MeatyMod.Cli.csproj -c Debug` must exit 0.
2. **verify** - `meatymod.exe verify "game\Blood and Bacon"` must exit 0 and its output must parse (regex) to `Valid: 1860` and `Invalid: 0`.
3. **manifest** - `meatymod.exe manifest "game\Blood and Bacon\Content" <out.json>` must exit 0, write the JSON file, and the JSON must have the expected key count.
4. **parse (dialogue)** - `meatymod.exe parse "game\Blood and Bacon\ABCDE3\2.txt"` must exit 0 and print a `Lines:` count.
5. **parse (camera)** - `meatymod.exe parse "game\Blood and Bacon\ABCDE3\camMove1.txt"` must exit 0.
6. **raw heightmaps** - `Content\astro\brushes\earth.raw` and `earth2.raw` must exist, each exactly 8,000,000 bytes (`Get-Item .Length`).
7. **wmv count** - exactly 4 `.wmv` files under the game tree (`Get-ChildItem -Recurse`).
8. **Content xnb file count** - exactly 1396 `.xnb` files under `Content\` (`Get-ChildItem -Recurse`).

The CLI is invoked via the built binary directly (`src\MeatyMod.Cli\bin\Debug\net10.0\meatymod.exe`) to avoid `dotnet run --` argument-parsing issues. PowerShell 5.1 built-ins only.

## Baselines
| Baseline | Value |
| --- | --- |
| Total `.xnb` under game tree | 1860 |
| `.xnb` under `Content\` | 1396 |
| `.txt` under game tree | 709 |
| `.txt` under `ABCDE3\` | 676 |
| `.raw` heightmaps | 2 |
| `earth.raw` / `earth2.raw` size | 8,000,000 bytes each |
| `.wmv` | 4 |

## Results - 2026-08-06

| Check | Baseline | Observed | Status |
| --- | --- | --- | --- |
| dotnet build (Debug) | exit 0 | exit 0 (234 pre-existing warnings, 0 errors) | PASS |
| verify | Valid 1860, Invalid 0 | `Valid: 1860 Invalid: 0 Total: 1860`, exit 0 | PASS |
| manifest key count | 1353 unique keys | 1353 | PASS |
| manifest writes out.json | file created | yes | PASS |
| parse ABCDE3\2.txt | exit 0, prints Lines | exit 0, `Lines: 59` | PASS |
| parse ABCDE3\camMove1.txt | exit 0 | exit 0, `Lines: 67` | PASS |
| earth.raw size | 8000000 | 8000000 | PASS |
| earth2.raw size | 8000000 | 8000000 | PASS |
| wmv count | 4 | 4 | PASS |
| `.xnb` under Content | 1396 | 1396 | PASS |
| **Overall** | | | **SMOKE PASS** |

## Manifest Key Count Note
The task-specified manifest baseline was 1396 keys (assumed one key per `.xnb` file). The CLI's `AssetManifestBuilder` keys the manifest by file base name (`Path.GetFileNameWithoutExtension`), so files that share a base name collapse into one key:

- 1396 `.xnb` files under `Content\`
- 1352 unique base names (case-insensitive)
- 1353 unique base names (case-sensitive; `opening` vs `Opening` differ only by case and produce two keys)

The observed manifest key count is therefore **1353**, not 1396. Source files are out of scope for this task (never modified), so the smoke test asserts the tool's actual contract (1353 keys) and separately asserts the 1396-file count so neither fact regresses unnoticed.

## Notes
- Game tree is read-only; the smoke test only reads and asserts. The temporary `out.json` is written to `$env:TEMP` and removed after the check.
- One transient `CS0103 IsInside` build failure was seen on the first run; a clean rebuild succeeded and subsequent runs were stable (SMOKE PASS twice in a row).
- Verdict: PASS
