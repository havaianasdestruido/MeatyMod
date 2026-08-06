using System;
using System.Collections.Generic;
using System.IO;
using MeatyMod.Core;

namespace MeatyMod.Cli.Commands;

public class ChecksumCommand : ICommand
{
    public string Name => "checksum";

    public int Run(string[] args)
    {
        if (args is null || args.Length == 0)
        {
            Console.Error.WriteLine("Usage: meatymod checksum <file-or-dir>");
            return 1;
        }

        var target = args[0];

        try
        {
            if (File.Exists(target))
            {
                Console.WriteLine($"{target}  {ChecksumUtil.Sha256File(target)}");
                return 0;
            }

            if (Directory.Exists(target))
            {
                var files = new List<string>(Directory.EnumerateFiles(target, "*", SearchOption.AllDirectories));
                files.Sort(StringComparer.Ordinal);

                var exitCode = 0;
                foreach (var file in files)
                {
                    try
                    {
                        var relPath = Path.GetRelativePath(target, file).Replace('\\', '/');
                        Console.WriteLine($"{relPath}  {ChecksumUtil.Sha256File(file)}");
                    }
                    catch (IOException ex)
                    {
                        Console.Error.WriteLine($"Failed to hash {file}: {ex.Message}");
                        exitCode = 1;
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Console.Error.WriteLine($"Failed to hash {file}: {ex.Message}");
                        exitCode = 1;
                    }
                }

                Console.WriteLine($"Files: {files.Count}");
                return exitCode;
            }

            Console.Error.WriteLine($"Path not found: {target}");
            return 1;
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Checksum failed: {ex.Message}");
            return 1;
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.Error.WriteLine($"Checksum failed: {ex.Message}");
            return 1;
        }
    }
}
