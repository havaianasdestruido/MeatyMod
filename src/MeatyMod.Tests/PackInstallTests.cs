using System;
using System.IO;
using System.IO.Compression;
using MeatyMod.Cli.Commands;
using MeatyMod.Core;

namespace MeatyMod.Tests;

public class PackInstallTests
{
    [Fact]
    public void PackAndInstall_RoundsTripToGameContent()
    {
        var tempRoot = CreateTempDir();
        var originalCwd = Directory.GetCurrentDirectory();
        try
        {
            var modDir = Path.Combine(tempRoot, "Mod");
            Directory.CreateDirectory(modDir);
            File.WriteAllText(Path.Combine(modDir, "manifest.json"),
                "{\"Id\":\"test\",\"Name\":\"Test\",\"Version\":\"1.0.0\",\"Author\":\"a\",\"Description\":\"d\",\"Replaces\":[]}");
            File.WriteAllText(Path.Combine(modDir, "level1.bin"), "content-bytes");
            File.WriteAllBytes(Path.Combine(modDir, "Mod.dll"), "MZ"u8);

            Directory.SetCurrentDirectory(tempRoot);
            var packResult = new PackCommand().Run(new[] { modDir });
            Assert.Equal(0, packResult);

            var zipPath = Path.Combine(tempRoot, "mod.zip");
            Assert.True(File.Exists(zipPath));

            var gameDir = Path.Combine(tempRoot, "Game");
            Directory.CreateDirectory(gameDir);
            var installResult = new InstallCommand().Run(new[] { zipPath, gameDir });
            Assert.Equal(0, installResult);

            Assert.True(File.Exists(Path.Combine(gameDir, "Content", "manifest.json")));
            Assert.True(File.Exists(Path.Combine(gameDir, "Content", "level1.bin")));
            Assert.True(File.Exists(Path.Combine(gameDir, "Content", "Mod.dll")));
            Assert.Equal("content-bytes", File.ReadAllText(Path.Combine(gameDir, "Content", "level1.bin")));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void Pack_SkipsOversizedFile()
    {
        var tempRoot = CreateTempDir();
        var originalCwd = Directory.GetCurrentDirectory();
        var originalError = Console.Error;
        try
        {
            var modDir = Path.Combine(tempRoot, "Mod");
            Directory.CreateDirectory(modDir);
            File.WriteAllText(Path.Combine(modDir, "small.txt"), "ok");
            var hugePath = Path.Combine(modDir, "huge.bin");
            using var hugeFs = new FileStream(hugePath, FileMode.CreateNew);
            hugeFs.SetLength(FileSizeGuard.DefaultMaxBytes + 1);

            Directory.SetCurrentDirectory(tempRoot);
            using var error = new StringWriter();
            Console.SetError(error);
            var result = new PackCommand().Run(new[] { modDir });

            Assert.Equal(0, result);
            Assert.Contains("Skipping oversized", error.ToString());
            using var archive = ZipFile.OpenRead(Path.Combine(tempRoot, "mod.zip"));
            Assert.Contains(archive.Entries, e => e.FullName == "small.txt");
            Assert.DoesNotContain(archive.Entries, e => e.FullName == "huge.bin");
        }
        finally
        {
            Console.SetError(originalError);
            Directory.SetCurrentDirectory(originalCwd);
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void Install_SkipsOversizedEntry()
    {
        var tempRoot = CreateTempDir();
        var originalCwd = Directory.GetCurrentDirectory();
        var originalError = Console.Error;
        try
        {
            Directory.SetCurrentDirectory(tempRoot);
            var zipPath = Path.Combine(tempRoot, "big.zip");
            using (var fs = File.Create(zipPath))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                var ok = archive.CreateEntry("ok.txt");
                using (var writer = new StreamWriter(ok.Open()))
                {
                    writer.Write("hi");
                }

                var big = archive.CreateEntry("big.bin", CompressionLevel.Fastest);
                using (var bigStream = big.Open())
                {
                    var buf = new byte[1024 * 1024];
                    for (int i = 0; i < 101; i++)
                    {
                        bigStream.Write(buf, 0, buf.Length);
                    }
                }
            }

            var gameDir = Path.Combine(tempRoot, "Game");
            Directory.CreateDirectory(gameDir);
            using var error = new StringWriter();
            Console.SetError(error);
            var result = new InstallCommand().Run(new[] { zipPath, gameDir });

            Assert.Equal(0, result);
            Assert.Contains("Skipping oversized", error.ToString());
            Assert.True(File.Exists(Path.Combine(gameDir, "Content", "ok.txt")));
            Assert.False(File.Exists(Path.Combine(gameDir, "Content", "big.bin")));
        }
        finally
        {
            Console.SetError(originalError);
            Directory.SetCurrentDirectory(originalCwd);
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void Pack_ExcludesNestedBinObjAndDotfiles()
    {
        var tempRoot = CreateTempDir();
        var originalCwd = Directory.GetCurrentDirectory();
        try
        {
            var modDir = Path.Combine(tempRoot, "Mod");
            Directory.CreateDirectory(modDir);
            File.WriteAllText(Path.Combine(modDir, "config.txt"), "x");
            Directory.CreateDirectory(Path.Combine(modDir, "src", "Mod", "bin", "Release"));
            File.WriteAllText(Path.Combine(modDir, "src", "Mod", "bin", "Release", "Mod.dll"), "MZ");
            Directory.CreateDirectory(Path.Combine(modDir, "src", "Mod", "obj"));
            File.WriteAllText(Path.Combine(modDir, "src", "Mod", "obj", "Mod.pdb"), "dbg");
            File.WriteAllText(Path.Combine(modDir, ".gitignore"), "bin/");
            File.WriteAllText(Path.Combine(modDir, "src", "Mod", "Mod.cs"), "class {}");

            Directory.SetCurrentDirectory(tempRoot);
            var result = new PackCommand().Run(new[] { modDir });
            Assert.Equal(0, result);

            using var archive = ZipFile.OpenRead(Path.Combine(tempRoot, "mod.zip"));
            var names = archive.Entries.Select(e => e.FullName).ToList();
            Assert.Contains("config.txt", names);
            Assert.Contains("src/Mod/Mod.cs", names);
            Assert.DoesNotContain(names, n => n.Contains("bin/") || n.Contains("obj/") || n.StartsWith(".gitignore"));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void Pack_ChecksumsTxtMatchesChecksumCommandFormat()
    {
        var tempRoot = CreateTempDir();
        var originalCwd = Directory.GetCurrentDirectory();
        try
        {
            var modDir = Path.Combine(tempRoot, "Mod");
            Directory.CreateDirectory(modDir);
            File.WriteAllText(Path.Combine(modDir, "config.txt"), "x");
            Directory.CreateDirectory(Path.Combine(modDir, "sub"));
            File.WriteAllText(Path.Combine(modDir, "sub", "asset.bin"), "data");

            Directory.SetCurrentDirectory(tempRoot);
            var result = new PackCommand().Run(new[] { modDir, Path.Combine(tempRoot, "m.zip") });
            Assert.Equal(0, result);

            using var archive = ZipFile.OpenRead(Path.Combine(tempRoot, "m.zip"));
            var checksumEntry = archive.Entries.First(e => e.FullName == "checksums.txt");
            using var reader = new StreamReader(checksumEntry.Open());
            var zipLines = reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim()).ToList();

            var cmdOut = new StringWriter();
            var originalOut = Console.Out;
            Console.SetOut(cmdOut);
            try
            {
                new ChecksumCommand().Run(new[] { modDir });
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            var cliLines = cmdOut.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(l => !l.TrimStart().StartsWith("Files:"))
                .Select(l => l.Trim()).ToList();

            Assert.Equal(cliLines.Count, zipLines.Count);
            foreach (var line in zipLines)
            {
                Assert.Contains(cliLines, l => l == line);
            }
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
            Directory.Delete(tempRoot, true);
        }
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "meaty_pack_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
