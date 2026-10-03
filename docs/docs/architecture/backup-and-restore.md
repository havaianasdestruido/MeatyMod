---
id: backup-and-restore
title: Backup and restore
sidebar_label: Backup & restore
description: The two independent backup mechanisms in MeatyMod and how to get a clean game back.
---

# Backup and restore

MeatyMod has **two unrelated backup mechanisms**. Knowing which one applies to what you did is the difference between a one-command fix and a Steam file verification.

| What you ran | What was backed up | Where | How to undo |
| --- | --- | --- | --- |
| `inject` | the whole executable | `<output-exe>.backup` | `meatymod restore <exe>` |
| `install` | each **overwritten** content file | `<game>\Backups\MeatyMod\<relative path>` | manual copy back |
| anything using `BackupManager` | one file at a time | `<backupRoot>\<path relative to CWD>` | `BackupManager.RestoreFile` |

## Executable backup (inject / restore)

`AssemblyInjector.Patch` copies the executable **before** it reads the module:

```csharp
File.Copy(exePath, backupPath, overwrite: true);
```

`InjectCommand` always passes `outputPath + ".backup"`, so an in-place patch of `BloodandBacon.exe` produces `BloodandBacon.exe.backup` next to it.

`RestoreCommand` is the mirror image and nothing more:

```csharp
var exePath    = Path.GetFullPath(args[0]);
var backupPath = exePath + ".backup";

if (!File.Exists(backupPath)) { /* "No backup found: …" */ return 1; }
File.Copy(backupPath, exePath, overwrite: true);
```

:::caution `overwrite: true` on the backup
The backup is overwritten on **every** inject. Injecting into an already-patched executable therefore stores a *patched* file as the "original". Always restore before re-injecting — every shipped `run.bat` and `launch-*.bat` starts with a silent `restore` for exactly this reason.
:::

Practical rules:

- The backup lives next to the output executable. If you pass an explicit output path, the backup follows the output, not the input.
- `restore` needs no game, no mods and no build state — only the two files.
- Steam's *Verify integrity of game files* is the fallback if both files are lost.

## Content backups (install)

`InstallCommand` extracts into `<game>\Content\` and, for each entry that would overwrite an existing file:

```csharp
var backupPath = Path.Combine(backupDir, Path.GetRelativePath(contentRoot, targetPath));
Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
File.Copy(targetPath, backupPath, overwrite: true);
Console.WriteLine($"Backed up {Path.GetRelativePath(contentRoot, targetPath)}");
```

where `backupDir` is `<game>\Backups\MeatyMod`.

- Only **overwritten** files are backed up; files the mod adds are left for you to delete.
- The mirror preserves the relative folder structure, so restoring is a recursive copy from `Backups\MeatyMod\` back over `Content\`.
- There is no `uninstall` command. To revert by hand:

  ```bat
  xcopy /E /Y "game\Blood and Bacon\Backups\MeatyMod\*" "game\Blood and Bacon\Content\"
  ```

## `BackupManager` (library API)

`MeatyMod.Core.BackupManager` is the general-purpose primitive used by tests and available to your own tooling. It is **not** what `install` uses.

```csharp
var backups = new BackupManager(@"C:\backups\meatymod");
backups.BackupFile(@"game\Blood and Bacon\Content\day1.txt");
backups.RestoreFile(@"game\Blood and Bacon\Content\day1.txt");
```

Behaviour that matters:

- Paths are mirrored **relative to `Directory.GetCurrentDirectory()`**, not to the file's own root. Change the working directory between backup and restore and the mapping changes with it.
- `ResolveBackupPath` rejects anything that escapes the backup root with `ArgumentException("Path resolves outside backup root: …")` — a `..\..\` relative path cannot write outside the root. The test is lexical: `Path.GetFullPath` plus an `OrdinalIgnoreCase` prefix comparison, which is correct on NTFS but does not follow symlinks or junctions, and on a case-sensitive filesystem would also accept a case-only sibling of the root. See the [security model](./security-model.md#path-containment-on-install-zip-slip).
- Backing up or restoring the working directory itself throws `ArgumentException`.
- Destination directories are created automatically; the source is opened `FileShare.Read` and the destination `FileShare.None`.

Full signatures in the [`MeatyMod.Core` API reference](../api/meatymod-core.md#backupmanager).

## Recovering a clean game

1. `meatymod restore "game\Blood and Bacon\BloodandBacon.exe"` — undoes the IL patch.
2. Delete the files `inject` deployed: `<Mod>.dll`, `<Mod>.txt`, `Content\<Mod>\`, plus `oink.log` / `quackmenu.log`.
3. Copy `Backups\MeatyMod\*` back over `Content\` if you ran `install`.
4. If anything is still wrong: Steam → *Properties* → *Installed Files* → *Verify integrity of game files*.
