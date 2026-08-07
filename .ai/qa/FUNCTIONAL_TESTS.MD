# Functional Testing Report

## Unit tests (dotnet test)
- 68/68 pass, 0 fail (observed via `dotnet test src\MeatyMod.Tests\MeatyMod.Tests.csproj`: 68 passed, 0 failed, total 68). Coverage: XNB header/content/LZX, TxtReader/CameraTrack/Dialogue, RawReader, Core (FileSizeGuard, ModManifestLoader, ChecksumUtil), ManifestValidation (14), Injector Patch (single/multi/order/auto-detect/missing-entry), BackupManager, Pack/Install integration, pack exclusion + checksum-format parity, plus 13 `InjectEdgeTests.cs` cases (missing exe/dll, array mismatch, missing myGame/ctor, empty mod list, legacy overload, in-place no-dll-corruption, InjectCommand_Legacy_DoesNotTreatDllAsOutput regression, multi-with-entry order, no/one-arg usage, RestoreCommand byte-identical).

## Extreme testing (2026-08-07)
- Patched-exe file audit:
  - Oink `AssemblyRef` token `[AssemblyRef:0x000e]`; ctor tail `IL_062 ldarg.0 / IL_063 call Oink.OinkEntry::Inject / IL_064 ret` — exactly 1 Inject ref and 1 ret.
  - Deployed `Oink.dll` SHA-256 matches source build; real game exe pristine; restore round-trip byte-exact.
- Headless ModHarness (`tools\modharness`): loads Oink.dll + XNA by reflection, constructs a `ShimGame` subclass of `Microsoft.Xna.Framework.Game` headlessly, invokes `OinkEntry.Inject`, verifies `OinkHook` added with `UpdateOrder == int.MaxValue`, `oink.log` written, hook `Update` runs without throw — 18 checks PASS. Requires x86 `PlatformTarget` (XNA is x86-only).
- Process memory viewer (`tools\memscan`): P/Invoke `OpenProcess`/`CreateToolhelp32Snapshot` + `Module32First/Next`, `VirtualQueryEx` + `ReadProcessMemory`; verified against a dummy target process (module found + heap marker found, UTF-16LE + UTF-8 scans).
- 68/68 tests, 0 failures.

## Rigorous manual testing (staging copy, game untouched)
- Build: `dotnet build src\MeatyMod.sln` — 0 errors.
- Smoke suite: `tools\smoke.ps1` — SMOKE PASS (verify 1860/0, manifest 1353 keys, raw 8000000, wmv 4).
- XNB sweep: `meatymod xnb "game\Blood and Bacon"` — Total 1860, Compressed 1159, Failed 0.
- Pack → install → restore cycle (Oink + QuackMenu, staging copy):
  - pack with output path, checksums.txt matches `meatymod checksum` line-for-line (0 mismatches);
  - zip excludes bin/, obj/, dotfiles;
  - install extracts to Content\, no unsafe-entry skips;
  - multi-mod inject: `.backup` byte-identical to pre-inject; both DLLs + config deployed; no temp files;
  - restore returns exe byte-identical to pre-inject (SHA-256 match);
  - IL probe: ctor contains exactly 2 Inject calls — Oink.OinkEntry.Inject then QuackMenu.QuackMenuEntry.Inject (in order).
- Edge cases: unknown command prints usage; missing path/file handled with exit 1; oversized file skipped in pack + install.

## Bugs found and fixed during testing
1. `pack` always overwrote `mod.zip` — added optional `[output.zip]` arg.
2. `checksums.txt` format (1 space, backslash paths) didn't match `checksum` command (2 spaces, forward slash) — unified on `relpath  sha256` forward-slash.
3. `pack` included nested `bin/`/`obj/` build artifacts and `.gitignore` — now excluded at any path depth.
4. In-place inject (`inject <exe> <dll>`, output == source) failed with file-in-use — injector now writes via temp file then replaces after module disposal.
5. **Legacy single-mod inject corrupted the mod DLL** — legacy parse added the DLL to positional args, so `outputPath` became the DLL path and `Patch` wrote the patched exe over `Oink.dll`. Legacy branch rewritten (DLL no longer treated as output). Rebuilt the corrupted DLL.

## Verdict
PASS.
