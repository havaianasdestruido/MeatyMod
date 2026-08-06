using System;
using System.IO;
using MeatyMod.Formats;

namespace MeatyMod.Cli.Commands
{
    public class ParseCommand : ICommand
    {
        public string Name => "parse";

        public int Run(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Usage: meatymod parse <file>");
                return 1;
            }

            string path = args[0];
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"File not found: {path}");
                return 1;
            }

            try
            {
                if (path.EndsWith(".raw", StringComparison.OrdinalIgnoreCase))
                {
                    return RunRaw(path);
                }

                TxtDocument doc = TxtReader.Read(path);

                if (doc.GetString(0).Contains(".") && doc.Count % 6 == 0)
                {
                    var keyframes = CameraTrackParser.Parse(path);
                    Console.WriteLine($"Camera keyframes: {keyframes.Count}");
                    int show = Math.Min(3, keyframes.Count);
                    for (int i = 0; i < show; i++)
                    {
                        var kf = keyframes[i];
                        Console.WriteLine($"Pos({kf.PosX}, {kf.PosY}, {kf.PosZ}) Target({kf.TargetX}, {kf.TargetY}, {kf.TargetZ})");
                    }
                }
                else
                {
                    Console.WriteLine($"Lines: {doc.Count}");
                    bool allNumeric = true;
                    for (int i = 0; i < doc.Count; i++)
                    {
                        string line = doc.GetString(i);
                        Console.WriteLine($"{i}: {line}");
                        if (!(int.TryParse(line, out _) || float.TryParse(line, out _)))
                        {
                            allNumeric = false;
                        }
                    }

                    if (allNumeric)
                    {
                        Console.WriteLine("All values numeric: True");
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Parse failed: {ex.Message}");
                return 1;
            }
        }

        private int RunRaw(string path)
        {
            bool guessed = RawReader.TryGuessDimensions(path, out int width, out int height);
            if (!guessed)
            {
                width = 2048;
                height = 2048;
            }

            RawHeightmap map = RawReader.Read(path, width, height);

            int min = map.Heights[0];
            int max = map.Heights[0];
            for (int i = 1; i < map.Heights.Length; i++)
            {
                int value = map.Heights[i];
                if (value < min) min = value;
                if (value > max) max = value;
            }

            Console.WriteLine($"Width: {map.Width}");
            Console.WriteLine($"Height: {map.Height}");
            Console.WriteLine($"SampleCount: {map.Heights.Length}");
            Console.WriteLine($"MinHeight: {min}");
            Console.WriteLine($"MaxHeight: {max}");
            return 0;
        }
    }
}
