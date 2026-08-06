using System;
using System.IO;
using MeatyMod.Core;

namespace MeatyMod.Tests;

public class BackupManagerTests
{
    [Fact]
    public void BackupFile_PreservesRelativeStructureFromCwd()
    {
        var tempRoot = CreateTempDir();
        var originalCwd = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(tempRoot);
            var contentDir = Path.Combine(tempRoot, "Content");
            Directory.CreateDirectory(contentDir);
            var source = Path.Combine(contentDir, "level1.txt");
            File.WriteAllText(source, "original");

            new BackupManager(Path.Combine(tempRoot, "Backups")).BackupFile(source);

            Assert.True(File.Exists(Path.Combine(tempRoot, "Backups", "Content", "level1.txt")));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void RestoreFile_RestoresOriginalContent()
    {
        var tempRoot = CreateTempDir();
        var originalCwd = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(tempRoot);
            var contentDir = Path.Combine(tempRoot, "Content");
            Directory.CreateDirectory(contentDir);
            var gameFile = Path.Combine(contentDir, "level1.txt");
            File.WriteAllText(gameFile, "original");
            var manager = new BackupManager(Path.Combine(tempRoot, "Backups"));
            manager.BackupFile(gameFile);

            File.WriteAllText(gameFile, "modified");
            manager.RestoreFile(gameFile);

            Assert.Equal("original", File.ReadAllText(gameFile));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void RestoreFile_Overwrite_KeepsLatestBackup()
    {
        var tempRoot = CreateTempDir();
        var originalCwd = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(tempRoot);
            var contentDir = Path.Combine(tempRoot, "Content");
            Directory.CreateDirectory(contentDir);
            var gameFile = Path.Combine(contentDir, "level1.txt");
            File.WriteAllText(gameFile, "v1");
            var manager = new BackupManager(Path.Combine(tempRoot, "Backups"));
            manager.BackupFile(gameFile);

            File.WriteAllText(gameFile, "v2");
            manager.BackupFile(gameFile);
            File.WriteAllText(gameFile, "corrupted");
            manager.RestoreFile(gameFile);

            Assert.Equal("v2", File.ReadAllText(gameFile));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
            Directory.Delete(tempRoot, true);
        }
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "meaty_backup_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
