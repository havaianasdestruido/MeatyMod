using System;
using System.IO;

namespace Oink
{
    public class OinkConfig
    {
        public string Enabled = "true";
        public string PigSkin = "true";
        public string SpeedMultiplier = "1.35";
        public string ToggleKey = "O";
        public string PigTexture = "npc/piggy1";

        public static OinkConfig Load()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "Oink", "config.txt");
                if (!File.Exists(path))
                {
                    path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "oink.txt");
                }
                if (File.Exists(path))
                {
                    var cfg = new OinkConfig();
                    foreach (var rawLine in File.ReadAllLines(path))
                    {
                        string line = rawLine.Trim();
                        if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";"))
                        {
                            continue;
                        }
                        int eq = line.IndexOf('=');
                        if (eq < 0)
                        {
                            continue;
                        }
                        string key = line.Substring(0, eq).Trim();
                        string value = line.Substring(eq + 1).Trim();
                        switch (key.ToLowerInvariant())
                        {
                            case "enabled": cfg.Enabled = value; break;
                            case "pigskin": cfg.PigSkin = value; break;
                            case "speedmultiplier": cfg.SpeedMultiplier = value; break;
                            case "togglekey": cfg.ToggleKey = value; break;
                            case "pigtexture": cfg.PigTexture = value; break;
                        }
                    }
                    return cfg;
                }
            }
            catch (Exception ex)
            {
                OinkEntry.Log("Config load failed: " + ex.Message);
            }
            return new OinkConfig();
        }
    }
}
