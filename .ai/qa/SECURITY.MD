# Security Verification Report
- Verification date: 2026-08-06.
- Verdict: PASS with documented residual risks.

## Controls in place
- InstallCommand extraction: boundary-aware containment check on every zip entry (`Path.GetFullPath(Path.Combine(contentRoot, entry.FullName))` must equal contentRoot or start with `contentRoot + DirectorySeparatorChar`). Directory entries skipped. Per-entry FileSizeGuard (100 MB cap) preserved. Extraction uses explicit `FileMode.Create` + `FileShare.Read`.
- BackupManager: `_backupRoot` normalized to full path; CWD-relative path must resolve inside backup root (else `ArgumentException`); empty-relative-path (source == CWD) rejected; streams use explicit `FileMode`/`FileShare`.
- PackCommand: entry names derived from `Path.GetRelativePath`, so pack output cannot contain `..` or absolute names; per-file oversized skip.
- InjectCommand: deployed destination filenames come from file basenames only (no traversal from archive data).

## Verification commands
- Crafted zip with traversal entries, installed into a temp destination via `new InstallCommand().Run(...)`:
  - `..\pwned.txt` -> rejected, no file written outside dest.
  - `..\..\..\..\..\escape.txt` -> rejected.
  - `sub\..\..\doubleescape.txt` -> rejected (collapsed traversal).
  - `C:\abs\evil.txt` -> rejected (absolute path).
  - `Content\ok.txt` -> installed to `<dest>\Content\Content\ok.txt` (legit entry unaffected).
- BackupManager guards via direct calls:
  - BackupFile of a file inside CWD -> mirrored under backup root (BACKUP_INSIDE_OK=True).
  - BackupFile of a path outside CWD (resolves via `..`) -> throws ArgumentException (BACKUP_OUTSIDE_REJECTED=True).
  - BackupFile of the CWD itself -> throws ArgumentException (BACKUP_CWD_REJECTED=True).

## Observed results
- Smoke harness exit code 0 (skip-on-unsafe-entry semantics preserved; unsafe entries logged to stderr).
- No file written outside the temp destination root by any malicious entry.
- `dotnet build src\MeatyMod.Core\MeatyMod.Core.csproj` and `dotnet build src\MeatyMod.Cli\MeatyMod.Cli.csproj`: 0 errors; edited files (InstallCommand.cs, BackupManager.cs) 0 warnings under repo analyzer config. Remaining build warnings originate from parallel agent T8's in-flight files and analyzer configuration.
- Test suite: 52 passed / 1 failed; the single failure is T8's ChecksumUtilTests file-lock flake that passes in isolation and is unrelated to these changes.

## Residual risks
- Zip header `Length` trusted for the oversized guard; multi-entry zip-bomb volume uncapped.
- Lexical containment only; pre-existing junctions under the destination root are followed by the OS.
- Backup files not checksum-verified on restore.
