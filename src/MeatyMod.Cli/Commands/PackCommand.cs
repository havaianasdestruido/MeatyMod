using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
            Console.Error.WriteLine("Usage: meaty pack <mod-directory>");
            return 1;
        }

        string modDir = args[0];
        if (!Directory.Exists(modDir))
        {
            Console.Error.WriteLine($"Directory not found: {modDir}");
            return 1;
        }

        string zipPath = Path.Combine(Environment.CurrentDirectory, "mod.zip");
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
                var relPath = Path.GetRelativePath(modDir, file).Replace('\\', '/');

                if (new FileInfo(file).Length > FileSizeGuard.DefaultMaxBytes)
                {
                    Console.Error.WriteLine($"Skipping oversized file: {relPath}");
                    continue;
                }

                archive.CreateEntryFromFile(file, relPath, CompressionLevel.Optimal);
                checksums.Add($"{relPath} {ChecksumUtil.Sha256File(file)}");
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
