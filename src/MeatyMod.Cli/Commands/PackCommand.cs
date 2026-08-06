using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using MeatyMod.Core;

namespace MeatyMod.Cli.Commands;

public class PackCommand : ICommand
{
    public string Name => "pack";

    public int Run(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: meatymod pack <mod-directory> [output.zip]");
            return 1;
        }

        string modDir = args[0];
        if (!Directory.Exists(modDir))
        {
            Console.Error.WriteLine($"Directory not found: {modDir}");
            return 1;
        }

        string zipPath = args.Length >= 2
            ? Path.GetFullPath(args[1])
            : Path.Combine(Environment.CurrentDirectory, "mod.zip");
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        try
        {
            using var fileStream = new FileStream(zipPath, FileMode.Create);
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create);

            var checksums = new List<string>();

            foreach (var file in Directory.EnumerateFiles(modDir, "*", SearchOption.AllDirectories))
            {
                var relPath = Path.GetRelativePath(modDir, file);
                var relNorm = relPath.Replace('\\', '/');
                var segments = relNorm.Split('/');

                if (segments.Any(s => string.Equals(s, "bin", StringComparison.OrdinalIgnoreCase)
                                      || string.Equals(s, "obj", StringComparison.OrdinalIgnoreCase))
                    || segments.Any(s => s.StartsWith(".", StringComparison.Ordinal)))
                {
                    continue;
                }

                if (new FileInfo(file).Length > FileSizeGuard.DefaultMaxBytes)
                {
                    Console.Error.WriteLine($"Skipping oversized file: {relPath}");
                    continue;
                }

                archive.CreateEntryFromFile(file, relNorm, CompressionLevel.Optimal);
                checksums.Add($"{relNorm}  {ChecksumUtil.Sha256File(file)}");
            }

            var checksumEntry = archive.CreateEntry("checksums.txt", CompressionLevel.Optimal);
            using (var writer = new StreamWriter(checksumEntry.Open(), new UTF8Encoding(false)))
            {
                foreach (var line in checksums)
                {
                    writer.WriteLine(line);
                }
            }

            Console.WriteLine($"Packed {modDir} -> {zipPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to pack mod: {ex.Message}");
            return 1;
        }
    }
}
