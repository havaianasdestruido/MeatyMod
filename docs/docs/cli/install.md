---
id: install
title: meatymod install
sidebar_label: install
description: Verify a mod zip and extract it into the game's Content folder, backing up anything it overwrites.
---

# `meatymod install`

Extracts a packed mod into `<game-path>\Content\`, after verifying its embedded checksums and rejecting unsafe entries.

```text
meatymod install <mod-zip> <game-path>
```

| Argument | Meaning |
| --- | --- |
| `<mod-zip>` | archive produced by [`pack`](./pack.md) |
| `<game-path>` | the game folder — **not** its `Content` subfolder |

## Example

```bat
meatymod install mod.zip "game\Blood and Bacon"
```

```text
Backed up npc/piggy1.xnb
Installed npc/piggy1.xnb
Installed manifest.json
Install complete.
```

## What it does

1. **Resolves paths.** `contentRoot` = `<game-path>\Content` (created if absent); `backupDir` = `<game-path>\Backups\MeatyMod`.
2. **Verifies integrity.** Reads `checksums.txt` from the archive and re-hashes every listed entry *before writing anything*. Any problem aborts the whole install:

   ```text
   Install aborted: mod integrity check failed.
     checksum mismatch: config.txt (expected 9ab0…, got 1f3c…)
   ```

   Reported problems: hash mismatch, a listed file missing from the archive, and `checksums.txt: malformed line: "…"`. Only the paths **listed** in `checksums.txt` are hashed, so an archive with no `checksums.txt` installs unverified, and entries the list omits are extracted unchecked. See [mod package](../formats/mod-package.md#how-install-uses-it).
3. **Extracts each entry**, skipping directory entries, and for each file:
   - **containment check** — `Path.GetFullPath(Path.Combine(contentRoot, entry.FullName))` must stay inside `contentRoot`, otherwise `Skipping unsafe entry: <name>`. This is a string comparison (`OrdinalIgnoreCase`, correct for NTFS): it blocks `..\` traversal and absolute paths, but does not follow symlinks or junctions, and on a case-sensitive filesystem a case-only sibling of the content root would pass. See the [security model](../architecture/security-model.md#path-containment-on-install-zip-slip);
   - **size check** — `entry.Length` must pass `FileSizeGuard.IsAllowed`, otherwise `Skipping oversized entry: <name>`;
   - **backup** — if the target already exists it is copied to `Backups\MeatyMod\<relative path>` and `Backed up <path>` is printed;
   - **write** — the entry stream is copied to the target with `FileMode.Create`.

Skips are per-entry and non-fatal: the remaining entries still install.

## Output

| Line | Stream | Meaning |
| --- | --- | --- |
| `Backed up <path>` | stdout | an existing file was copied to `Backups\MeatyMod\` |
| `Installed <entry>` | stdout | file written |
| `Install complete.` | stdout | finished |
| `Skipping unsafe entry: <entry>` | stderr | path escaped `Content\` |
| `Skipping oversized entry: <entry>` | stderr | entry over 100 MiB |
| `Install aborted: mod integrity check failed.` | stderr | checksum verification failed, nothing was written |
| `Install failed: <message>` | stderr | I/O or archive error |

## Exit codes

| Code | When |
| --- | --- |
| `0` | extraction finished (possibly with skipped entries) |
| `1` | fewer than two arguments, zip or game path missing, integrity failure, or an exception |

## Uninstalling

There is no `uninstall`. Copy the backups back:

```bat
xcopy /E /Y "game\Blood and Bacon\Backups\MeatyMod\*" "game\Blood and Bacon\Content\"
```

Files the mod *added* (as opposed to replaced) are not in the backup mirror and must be removed by hand. See [Backup and restore](../architecture/backup-and-restore.md#content-backups-install).

## Related

- [`pack`](./pack.md) — produces the archive and its checksums.
- [`verify`](./verify.md) — check the installed XNB files afterwards.
- [Security model](../architecture/security-model.md#path-containment-on-install-zip-slip).
