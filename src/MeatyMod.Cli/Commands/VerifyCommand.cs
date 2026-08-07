using System;
using System.IO;
using MeatyMod.Verifier;

namespace MeatyMod.Cli.Commands
{
    public class VerifyCommand : ICommand
    {
        public string Name => "verify";

        public int Run(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Usage: meatymod verify <xnb-file-or-dir>");
                return 1;
            }

            string path = args[0];
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                Console.Error.WriteLine($"Path not found: {path}");
                return 1;
            }

            try
            {
                if (File.Exists(path))
                {
                    bool valid = AssetValidator.ValidateXnb(path);
                    Console.WriteLine($"Valid: {valid}");
                    return valid ? 0 : 1;
                }

                var result = AssetValidator.ValidateDirectory(path);
                Console.WriteLine($"Valid: {result.Valid} Invalid: {result.Invalid} Total: {result.Total}");
                if (result.Total == 0)
                {
                    Console.Error.WriteLine("No XNB files found (empty content directory?)");
                    return 1;
                }
                return result.Valid == result.Total ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Verify failed: {ex.Message}");
                return 1;
            }
        }
    }
}
