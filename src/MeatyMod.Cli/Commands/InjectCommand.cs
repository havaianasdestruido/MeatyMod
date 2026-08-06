using System;
using System.IO;
using MeatyMod.Injector;

namespace MeatyMod.Cli.Commands
{
    public class InjectCommand : ICommand
    {
        public string Name => "inject";

        public int Run(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Usage: meatymod inject <game-exe> <mod-dll> [output-exe] [--entry <TypeName>]");
                return 1;
            }

            string entryTypeName = null;
            var entryIndex = Array.FindIndex(args, a => a == "--entry");
            if (entryIndex >= 0 && entryIndex + 1 < args.Length)
            {
                entryTypeName = args[entryIndex + 1];
            }

            var exePath = Path.GetFullPath(args[0]);
            var modDll = Path.GetFullPath(args[1]);
            var outputPath = args.Length >= 3 ? Path.GetFullPath(args[2]) : exePath;
            var backupPath = outputPath + ".backup";
            var outputDir = Path.GetDirectoryName(outputPath);

            try
            {
                AssemblyInjector.Patch(exePath, modDll, outputPath, backupPath, entryTypeName);

                File.Copy(modDll, Path.Combine(outputDir, Path.GetFileName(modDll)), overwrite: true);

                var configPath = Path.Combine(Path.GetDirectoryName(modDll), "..", "..", "..", "..", "..", "config.txt");
                configPath = Path.GetFullPath(configPath);
                if (File.Exists(configPath))
                {
                    File.Copy(configPath, Path.Combine(outputDir, "config.txt"), overwrite: true);
                }

                var modName = Path.GetFileNameWithoutExtension(modDll);
                if (File.Exists(configPath))
                {
                    File.Copy(configPath, Path.Combine(outputDir, modName + ".txt"), overwrite: true);
                    var contentDir = Path.Combine(outputDir, "Content", modName);
                    Directory.CreateDirectory(contentDir);
                    File.Copy(configPath, Path.Combine(contentDir, "config.txt"), overwrite: true);
                }

                Console.WriteLine($"Patched {outputPath}");
                Console.WriteLine($"Backup saved to {backupPath}");
                Console.WriteLine($"Mod deployed next to game exe.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Inject failed: {ex.Message}");
                return 1;
            }
        }
    }
}
