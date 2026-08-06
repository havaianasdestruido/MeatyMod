using System;
using System.IO;
using System.IO.Compression;
using MeatyMod.Core;

#pragma warning disable CA1515, CA1031 // Public ICommand classes and catch-all Run exit paths are the established CLI convention.
namespace MeatyMod.Cli.Commands;

public class InstallCommand : ICommand
{
    public string Name => "install";

    public int Run(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: meatymod install <mod-zip> <game-path>");
            return 1;
        }

        var modZip = Path.GetFullPath(args[0]);
        var gamePath = Path.GetFullPath(args[1]);

        if (!File.Exists(modZip))
        {
            Console.Error.WriteLine($"Mod zip not found: {modZip}");
            return 1;
        }

        if (!Directory.Exists(gamePath))
        {
            Console.Error.WriteLine($"Game path not found: {gamePath}");
            return 1;
        }

        var contentRoot = Path.GetFullPath(Path.Combine(gamePath, "Content"));
        var backupDir = Path.Combine(gamePath, "Backups", "MeatyMod");
        Directory.CreateDirectory(contentRoot);

        try
        {
            using var archive = ZipFile.OpenRead(modZip);
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.FullName) ||
                    entry.FullName.EndsWith('/') ||
                    entry.FullName.EndsWith('\\'))
                {
                    continue;
                }

                var targetPath = Path.GetFullPath(Path.Combine(contentRoot, entry.FullName));

                if (!IsInside(contentRoot, targetPath))
                {
                    Console.Error.WriteLine($"Skipping unsafe entry: {entry.FullName}");
                    continue;
                }

                if (!FileSizeGuard.IsAllowed(entry.Length))
                {
                    Console.Error.WriteLine($"Skipping oversized entry: {entry.FullName}");
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));

                if (File.Exists(targetPath))
                {
                    var backupPath = Path.Combine(backupDir, Path.GetRelativePath(contentRoot, targetPath));
                    Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
                    File.Copy(targetPath, backupPath, overwrite: true);
                    Console.WriteLine($"Backed up {Path.GetRelativePath(contentRoot, targetPath)}");
                }

#pragma warning disable CA5389 // entry.FullName is containment-validated against contentRoot above.
                using (var entryStream = entry.Open())
                using (var output = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    entryStream.CopyTo(output);
                }
#pragma warning restore CA5389

                Console.WriteLine($"Installed {entry.FullName}");
            }

            Console.WriteLine("Install complete.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Install failed: {ex.Message}");
            return 1;
        }
    }

    private static bool IsInside(string root, string path)
    {
        if (string.Equals(root, path, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return path.StartsWith(
            root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);
    }
}
