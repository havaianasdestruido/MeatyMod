using System;
using System.IO;

namespace MeatyMod.Cli.Commands
{
    public class RestoreCommand : ICommand
    {
        public string Name => "restore";

        public int Run(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("Usage: meatymod restore <patched-exe>");
                return 1;
            }

            var exePath = Path.GetFullPath(args[0]);
            var backupPath = exePath + ".backup";

            try
            {
                if (!File.Exists(backupPath))
                {
                    Console.Error.WriteLine($"No backup found: {backupPath}");
                    return 1;
                }
                File.Copy(backupPath, exePath, overwrite: true);
                Console.WriteLine($"Restored {exePath} from backup.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Restore failed: {ex.Message}");
                return 1;
            }
        }
    }
}
