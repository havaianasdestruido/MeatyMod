---
id: restore
title: meatymod restore
sidebar_label: restore
description: Copy the .backup file back over a patched game executable.
---

# `meatymod restore`

Restores a patched executable from the `.backup` written by [`inject`](./inject.md).

```text
meatymod restore <patched-exe>
```

## Example

```bat
meatymod restore "game\Blood and Bacon\BloodandBacon.exe"
```

```text
Restored C:\…\game\Blood and Bacon\BloodandBacon.exe from backup.
```

## Behaviour

The whole command is four lines of logic:

```csharp
var exePath    = Path.GetFullPath(args[0]);
var backupPath = exePath + ".backup";

if (!File.Exists(backupPath)) { Console.Error.WriteLine($"No backup found: {backupPath}"); return 1; }
File.Copy(backupPath, exePath, overwrite: true);
```

- The backup path is derived purely by appending `.backup` — you cannot point it somewhere else.
- The backup file is **not** deleted, so restore is idempotent and repeatable.
- Nothing else is cleaned up: the deployed `<Mod>.dll`, `<Mod>.txt`, `Content\<Mod>\` and `*.log` files stay where `inject` put them. They are inert once the IL call is gone.

## Exit codes

| Code | When |
| --- | --- |
| `0` | executable restored |
| `1` | no argument, no `.backup` next to the target, or `Restore failed: <message>` (file locked, permissions, …) |

:::tip Run it even when you are not sure
`restore` on an unpatched executable with a valid backup is harmless, which is why every `run.bat` and `launch-*.bat` begins with

```bat
"%MEATYMOD%" restore "%GAME_EXE%" >nul 2>&1
```

…before injecting, and repeats it after the game exits.
:::

## If the backup is gone

1. Check for `BloodandBacon.exe.backup` in the game folder (it is never deleted by MeatyMod).
2. Otherwise: Steam → *Blood & Bacon* → *Properties* → *Installed Files* → *Verify integrity of game files*.

## Related

- [Backup and restore](../architecture/backup-and-restore.md) — both backup mechanisms, and the content-mod case.
