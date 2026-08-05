using System;
using System.IO;
using System.IO.Compression;

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
            ZipFile.CreateFromDirectory(modDir, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
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
