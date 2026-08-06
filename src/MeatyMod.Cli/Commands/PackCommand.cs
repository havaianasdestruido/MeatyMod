using System;
using System.IO;
using System.IO.Compression;
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

            foreach (var file in Directory.EnumerateFiles(modDir, "*", SearchOption.AllDirectories))
            {
                var relPath = Path.GetRelativePath(modDir, file);

                if (new FileInfo(file).Length > FileSizeGuard.DefaultMaxBytes)
                {
                    Console.Error.WriteLine($"Skipping oversized file: {relPath}");
                    continue;
                }

                archive.CreateEntryFromFile(file, relPath.Replace('\\', '/'), CompressionLevel.Optimal);
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
