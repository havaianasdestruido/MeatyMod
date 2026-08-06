using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MeatyMod.Assets;

namespace MeatyMod.Cli.Commands
{
    public class ManifestCommand : ICommand
    {
        public string Name => "manifest";

        public int Run(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Usage: meatymod manifest <game-content-dir> [out.json]");
                return 1;
            }

            string dir = args[0];
            if (!Directory.Exists(dir))
            {
                Console.Error.WriteLine($"Directory not found: {dir}");
                return 1;
            }

            try
            {
                IDictionary<string, string> manifest = AssetManifestBuilder.Build(dir);

                if (args.Length > 1)
                {
                    string outPath = args[1];
                    string json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(outPath, json);
                    Console.WriteLine($"Manifest written to {outPath}");
                }
                else
                {
                    foreach (var entry in manifest)
                    {
                        Console.WriteLine($"{entry.Key} -> {entry.Value}");
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Manifest failed: {ex.Message}");
                return 1;
            }
        }
    }
}
