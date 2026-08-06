# Security Maintenance Log
- Review date: 2026-08-06.
- Scope: InstallCommand zip extraction (FIXED), BackupManager (FIXED), PackCommand pack (reviewed), InjectCommand config deploy (reviewed), AssemblyInjector backup copy (reviewed). PackCommand/InjectCommand/AssemblyInjector owned by parallel agent T8; not modified.
- Baseline: path traversal normalized, XNB magic validated, oversized-file guard present.
- No vulnerabilities disclosed since baseline.

## Reviewed components
- InstallCommand.cs extraction: zip-slip and traversal guards hardened.
- BackupManager.cs: backup/restore path containment hardened.
- PackCommand.cs: pack output uses `Path.GetRelativePath(modDir, file)` for entry names; no `..` or absolute entry names can be produced; per-file oversized skip via FileSizeGuard.
- InjectCommand.cs: deployed filenames derived from `Path.GetFileName(dll)` / `Path.GetFileNameWithoutExtension(dll)` only; config source is a fixed upward walk from the mod DLL (by-design layout), not archive data.
- AssemblyInjector.cs: backup copy of the exe to `<output>.backup` before patching; overwrite explicit.

## Threats assessed
- Zip-slip / archive path traversal: FIXED. InstallCommand normalizes `Path.Combine(contentRoot, entry.FullName)` and enforces a boundary-aware containment check (full path must equal root or start with `root + DirectorySeparatorChar`), rejecting `..` traversal, absolute entry names, and prefix-sibling escapes (e.g. `..\..\Content2\evil` resolving to a sibling directory that shares the `Content` prefix). Unsafe entries are skipped with a warning; exit codes unchanged.
- Absolute entry paths (e.g. `C:\evil.txt`): FIXED (rejected by the same containment check).
- Directory entries in zips: handled (skipped) instead of being extracted as empty files or throwing.
- Symlink / junction following during zip extraction: MITIGATED. .NET `ZipArchive` does not materialize symlink entries, so zip-level symlink escape is not possible. Pre-existing junctions/reparse points inside the destination tree are followed by the OS when writing; containment is lexical, not filesystem-level. Dest install targets are user-supplied dirs; `game\Blood and Bacon` is read-only and never written.
- Backup overwrite / replay: MITIGATED. BackupManager writes with explicit `FileMode.Create`/`FileShare.None` and overwrite semantics; inject writes exe backup before patching. RestoreCommand has no integrity check on the backup file; a tampered/replayed `.backup` would be restored as-is.
- Backup path traversal: FIXED. BackupManager computes the CWD-relative path and rejects any resolved path outside the backup root; backing up the working directory itself (empty relative path ".") is rejected.
- Oversized-file DoS: MITIGATED (pre-existing FileSizeGuard, preserved). Per-entry uncompressed length capped at 100 MB; oversized entries skipped. Zip-bomb expansion is still bounded per-entry only, and the guard trusts the zip header's declared length.

## Status
- Fixed: zip-slip + path traversal (InstallCommand), backup-root traversal + empty-relative-path + explicit FileShare/overwrite (BackupManager).
- Mitigated: symlink/junction following (documented), backup replay (documented), oversized-file DoS (per-entry cap only).
- Documentation only: PackCommand, InjectCommand, AssemblyInjector (owned by T8).

## Residual risks
- FileSizeGuard trusts the zip header `Length` (uncompressed size); a crafted header under 100 MB whose payload expands beyond it is not caught mid-stream. Total expanded volume across many entries is uncapped.
- Lexical containment does not defend against pre-existing junctions/symlinks under the destination root.
- Backup files (`.backup` sidecar and BackupManager tree) are not checksum-verified on restore.
- Skip-on-unsafe-entry keeps exit code 0 even when entries were dropped; operators must read the "Skipping unsafe entry" output.
- Analyzer CA5389 (zip path sanitize) is suppressed in InstallCommand with justification: the containment check preceding extraction is the sanitization.
