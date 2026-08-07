using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using MeatyMod.Cli.Commands;
using MeatyMod.Core;

namespace MeatyMod.Tests;

public class InstallChecksumTests
{
    [Fact]
    public void Install_WithMatchingChecksums_Installs()
    {
        var tempRoot = CreateTempDir();
        var originalCwd = Directory.GetCurrentDirectory();
        try
        {
            var content = Encoding.UTF8.GetBytes("hello");
            var hash = ChecksumUtil.Sha256(content);

            Directory.SetCurrentDirectory(tempRoot);
            var zipPath = Path.Combine(tempRoot, "mod.zip");
            using (var fs = File.Create(zipPath))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                WriteBytes(archive, "manifest.json", content);
                WriteBytes(archive, "checksums.txt", Encoding.UTF8.GetBytes($"manifest.json  {hash}\n"));
            }

            var gameDir = Path.Combine(tempRoot, "Game");
            Directory.CreateDirectory(gameDir);
            var result = new InstallCommand().Run(new[] { zipPath, gameDir });

            Assert.Equal(0, result);
            Assert.Equal("hello", File.ReadAllText(Path.Combine(gameDir, "Content", "manifest.json")));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void Install_TamperedFile_FailsWithoutPartialExtraction()
    {
        var tempRoot = CreateTempDir();
        var originalCwd = Directory.GetCurrentDirectory();
        var originalError = Console.Error;
        try
        {
            var good = Encoding.UTF8.GetBytes("good");
            var bad = Encoding.UTF8.GetBytes("tampered!");
            var goodHash = ChecksumUtil.Sha256(good);
            var wrongHash = ChecksumUtil.Sha256(Encoding.UTF8.GetBytes("something-else"));

            Directory.SetCurrentDirectory(tempRoot);
            var zipPath = Path.Combine(tempRoot, "mod.zip");
            using (var fs = File.Create(zipPath))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                WriteBytes(archive, "good.txt", good);
                WriteBytes(archive, "bad.txt", bad);
                WriteBytes(archive, "checksums.txt", Encoding.UTF8.GetBytes($"good.txt  {goodHash}\nbad.txt  {wrongHash}\n"));
            }

            var gameDir = Path.Combine(tempRoot, "Game");
            Directory.CreateDirectory(gameDir);
            using var error = new StringWriter();
            Console.SetError(error);
            var result = new InstallCommand().Run(new[] { zipPath, gameDir });

            Assert.Equal(1, result);
            Assert.Contains("bad.txt", error.ToString());
            Assert.Contains("checksum", error.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(Path.Combine(gameDir, "Content", "good.txt")));
            Assert.False(File.Exists(Path.Combine(gameDir, "Content", "bad.txt")));
        }
        finally
        {
            Console.SetError(originalError);
            Directory.SetCurrentDirectory(originalCwd);
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void Install_MissingListedFile_Fails()
    {
        var tempRoot = CreateTempDir();
        var originalCwd = Directory.GetCurrentDirectory();
        var originalError = Console.Error;
        try
        {
            var content = Encoding.UTF8.GetBytes("present");
            var hash = ChecksumUtil.Sha256(content);

            Directory.SetCurrentDirectory(tempRoot);
            var zipPath = Path.Combine(tempRoot, "mod.zip");
            using (var fs = File.Create(zipPath))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                WriteBytes(archive, "present.txt", content);
                WriteBytes(archive, "checksums.txt", Encoding.UTF8.GetBytes($"ghost.txt  {hash}\n"));
            }

            var gameDir = Path.Combine(tempRoot, "Game");
            Directory.CreateDirectory(gameDir);
            using var error = new StringWriter();
            Console.SetError(error);
            var result = new InstallCommand().Run(new[] { zipPath, gameDir });

            Assert.Equal(1, result);
            Assert.Contains("ghost.txt", error.ToString());
            Assert.False(File.Exists(Path.Combine(gameDir, "Content", "present.txt")));
        }
        finally
        {
            Console.SetError(originalError);
            Directory.SetCurrentDirectory(originalCwd);
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void Install_WithoutChecksumsTxt_StillInstalls()
    {
        var tempRoot = CreateTempDir();
        var originalCwd = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(tempRoot);
            var zipPath = Path.Combine(tempRoot, "mod.zip");
            using (var fs = File.Create(zipPath))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                WriteBytes(archive, "asset.bin", Encoding.UTF8.GetBytes("data"));
            }

            var gameDir = Path.Combine(tempRoot, "Game");
            Directory.CreateDirectory(gameDir);
            var result = new InstallCommand().Run(new[] { zipPath, gameDir });

            Assert.Equal(0, result);
            Assert.True(File.Exists(Path.Combine(gameDir, "Content", "asset.bin")));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
            Directory.Delete(tempRoot, true);
        }
    }

    private static void WriteBytes(ZipArchive archive, string name, byte[] bytes)
    {
        var entry = archive.CreateEntry(name);
        using var stream = entry.Open();
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "meaty_checksum_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
