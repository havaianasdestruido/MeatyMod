using System;
using System.IO;
using System.IO.Compression;
using MeatyMod.Core;

namespace MeatyMod.Cli.Commands
{
    public class InstallCommand : ICommand
    {
        public string Name => "install";

        public int Run(string[] args)
        {
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

            var contentDir = Path.Combine(gamePath, "Content");
            var backupDir = Path.Combine(gamePath, "Backups", "MeatyMod");
            Directory.CreateDirectory(contentDir);

            try
            {
                using var archive = ZipFile.OpenRead(modZip);
                foreach (var entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        continue;
                    }

                    var targetPath = Path.GetFullPath(Path.Combine(contentDir, entry.FullName));

                    if (!targetPath.StartsWith(contentDir, StringComparison.OrdinalIgnoreCase))
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
                        var backupPath = Path.Combine(backupDir, Path.GetRelativePath(contentDir, targetPath));
                        Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
                        File.Copy(targetPath, backupPath, overwrite: true);
                        Console.WriteLine($"Backed up {Path.GetRelativePath(contentDir, targetPath)}");
                    }

                    entry.ExtractToFile(targetPath, overwrite: true);
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
    }
}
