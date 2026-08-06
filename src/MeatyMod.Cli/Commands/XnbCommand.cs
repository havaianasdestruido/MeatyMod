using System;
using System.IO;
using MeatyMod.Formats;

namespace MeatyMod.Cli.Commands
{
    public class XnbCommand : ICommand
    {
        public string Name => "xnb";

        public int Run(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Usage: meatymod xnb <xnb-file-or-dir>");
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
                    DumpFile(path);
                    return 0;
                }

                return SweepDirectory(path);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Xnb failed: {ex.Message}");
                return 1;
            }
        }

        private static void DumpFile(string path)
        {
            XnbContent content = XnbContentReader.Read(path);
            Console.WriteLine($"Magic: {content.Magic}");
            Console.WriteLine($"Platform: 0x{content.Platform:X2} ({(char)content.Platform})");
            Console.WriteLine($"Version: {content.Version}");
            Console.WriteLine($"Flags: 0x{content.Flags:X2}");
            Console.WriteLine($"Compressed: {content.IsCompressed}");
            Console.WriteLine($"DecompressedSize: {content.DecompressedSize}");
            Console.WriteLine($"ContentBytes: {content.Content.Length}");
            if (content.IsCompressed)
            {
                Console.WriteLine($"TypeId: {TypeIdHex(content.Content)}");
            }
        }

        private static int SweepDirectory(string path)
        {
            int total = 0;
            int compressed = 0;
            int failed = 0;

            foreach (string file in Directory.GetFiles(path, "*.xnb", SearchOption.AllDirectories))
            {
                total++;
                try
                {
                    XnbContent content = XnbContentReader.Read(file);
                    string relative = Path.GetRelativePath(path, file);
                    Console.WriteLine($"{relative}: platform 0x{content.Platform:X2} version {content.Version} flags 0x{content.Flags:X2} compressed={content.IsCompressed}");
                    if (content.IsCompressed)
                    {
                        compressed++;
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    Console.Error.WriteLine($"Failed: {file}: {ex.Message}");
                }
            }

            Console.WriteLine($"Total: {total} Compressed: {compressed} Failed: {failed}");
            return 0;
        }

        private static string TypeIdHex(byte[] content)
        {
            string hex = string.Empty;
            int count = Math.Min(4, content.Length);
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    hex += " ";
                }
                hex += content[i].ToString("X2");
            }
            return hex;
        }
    }
}
