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
                Console.Error.WriteLine("Usage: meatymod parse <txt-file>");
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
    }
}
