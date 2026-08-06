using System;
using System.IO;

namespace MeatyMod.Core;

public class BackupManager(string backupRoot)
{
    private readonly string _backupRoot = Path.GetFullPath(backupRoot);

    public void BackupFile(string sourcePath)
    {
        var sourceFull = Path.GetFullPath(sourcePath);
        var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), sourceFull);

        if (string.IsNullOrEmpty(relativePath) || relativePath == ".")
        {
            throw new ArgumentException($"Cannot back up the working directory itself: {sourceFull}");
        }

        var destPath = ResolveBackupPath(relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

        using var source = new FileStream(sourceFull, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var destination = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None);
        source.CopyTo(destination);
    }

    public void RestoreFile(string gamePath)
    {
        var gameFull = Path.GetFullPath(gamePath);
        var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), gameFull);

        if (string.IsNullOrEmpty(relativePath) || relativePath == ".")
        {
            throw new ArgumentException($"Cannot restore the working directory itself: {gameFull}");
        }

        var backupPath = ResolveBackupPath(relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(gameFull) ?? ".");

        using var source = new FileStream(backupPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var destination = new FileStream(gameFull, FileMode.Create, FileAccess.Write, FileShare.None);
        source.CopyTo(destination);
    }

    private string ResolveBackupPath(string relativePath)
    {
        var destPath = Path.GetFullPath(Path.Combine(_backupRoot, relativePath));
        var rootPrefix = _backupRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (!destPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Path resolves outside backup root: {relativePath}");
        }

        return destPath;
    }
}
