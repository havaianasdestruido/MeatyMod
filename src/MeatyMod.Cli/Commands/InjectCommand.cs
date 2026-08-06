using System;
using System.Collections.Generic;
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
                Console.Error.WriteLine("Usage: meatymod inject <game-exe> [--mod <dll> [--entry <TypeName>]]... [output-exe]");
                return 1;
            }

            try
            {
                var modDllPaths = new List<string>();
                var entryTypeNames = new List<string>();
                var positional = new List<string>();

                bool hasModFlag = false;
                foreach (var a in args)
                {
                    if (a == "--mod")
                    {
                        hasModFlag = true;
                        break;
                    }
                }

                if (!hasModFlag)
                {
                    positional.Add(args[0]);
                    positional.Add(args[1]);
                    if (args.Length >= 3 && args[2] != "--entry")
                    {
                        positional.Add(args[2]);
                    }
                    modDllPaths.Add(Path.GetFullPath(args[1]));
                    string entry = null;
                    var entryIndex = Array.FindIndex(args, a => a == "--entry");
                    if (entryIndex >= 0 && entryIndex + 1 < args.Length)
                    {
                        entry = args[entryIndex + 1];
                    }
                    entryTypeNames.Add(entry);
                }
                else
                {
                    int current = -1;
                    string pendingEntry = null;
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (args[i] == "--mod")
                        {
                            if (i + 1 >= args.Length)
                            {
                                Console.Error.WriteLine("Missing DLL path after --mod.");
                                return 1;
                            }
                            if (pendingEntry != null)
                            {
                                entryTypeNames[current] = pendingEntry;
                                pendingEntry = null;
                            }
                            modDllPaths.Add(Path.GetFullPath(args[i + 1]));
                            entryTypeNames.Add(null);
                            current = modDllPaths.Count - 1;
                            i++;
                        }
                        else if (args[i] == "--entry")
                        {
                            if (i + 1 >= args.Length)
                            {
                                Console.Error.WriteLine("Missing type name after --entry.");
                                return 1;
                            }
                            if (current >= 0)
                            {
                                entryTypeNames[current] = args[i + 1];
                            }
                            else
                            {
                                pendingEntry = args[i + 1];
                            }
                            i++;
                        }
                        else
                        {
                            positional.Add(args[i]);
                        }
                    }
                    if (pendingEntry != null && current >= 0)
                    {
                        entryTypeNames[current] = pendingEntry;
                    }
                }

                if (positional.Count < 1)
                {
                    Console.Error.WriteLine("Usage: meatymod inject <game-exe> [--mod <dll> [--entry <TypeName>]]... [output-exe]");
                    return 1;
                }
                if (modDllPaths.Count == 0)
                {
                    Console.Error.WriteLine("No mod DLL specified.");
                    return 1;
                }

                var exePath = Path.GetFullPath(positional[0]);
                var outputPath = positional.Count >= 2 ? Path.GetFullPath(positional[positional.Count - 1]) : exePath;
                var backupPath = outputPath + ".backup";
                var outputDir = Path.GetDirectoryName(outputPath);

                var modDllArray = modDllPaths.ToArray();
                var entryArray = entryTypeNames.ToArray();

                AssemblyInjector.Patch(exePath, modDllArray, entryArray, outputPath, backupPath);

                var deployed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < modDllArray.Length; i++)
                {
                    var dll = modDllArray[i];
                    var dllName = Path.GetFileName(dll);
                    if (!deployed.Add(dllName))
                    {
                        Console.Error.WriteLine($"Warning: duplicate mod DLL filename {dllName}, skipping deploy of {dll}");
                        continue;
                    }
                    File.Copy(dll, Path.Combine(outputDir, dllName), overwrite: true);

                    var configPath = Path.Combine(Path.GetDirectoryName(dll), "..", "..", "..", "..", "..", "config.txt");
                    configPath = Path.GetFullPath(configPath);
                    if (!File.Exists(configPath))
                    {
                        continue;
                    }

                    var modName = Path.GetFileNameWithoutExtension(dll);
                    File.Copy(configPath, Path.Combine(outputDir, modName + ".txt"), overwrite: true);
                    var contentDir = Path.Combine(outputDir, "Content", modName);
                    Directory.CreateDirectory(contentDir);
                    File.Copy(configPath, Path.Combine(contentDir, "config.txt"), overwrite: true);
                }

                Console.WriteLine($"Patched {outputPath}");
                Console.WriteLine($"Backup saved to {backupPath}");
                Console.WriteLine("Mods deployed next to game exe.");
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
