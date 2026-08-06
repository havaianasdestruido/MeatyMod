# Functional Testing Report

## Unit tests (dotnet test)
- 55/55 pass. Coverage: XNB header/content/LZX, TxtReader/CameraTrack/Dialogue, RawReader, Core (FileSizeGuard, ModManifestLoader, ChecksumUtil), ManifestValidation (14), Injector Patch (single/multi/order/auto-detect/missing-entry), BackupManager, Pack/Install integration, pack exclusion + checksum-format parity.

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
