using System;
using System.IO;
using System.Text;

namespace QuackMenu
{
    public class QuackConfig
    {
        public string CreativeModeEnabled = "true";
        public string FlatWorld = "true";
        public string SpawnHeight = "3";
        public string OpenMenuKey = "F1";
        public string[] Bosses = new[] { "Cutty", "Princess", "BoarKing", "Twin" };
        public string[] BossWeights = new[] { "0", "1", "2", "3" };

        public static QuackConfig Load()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "QuackMenu", "config.txt");
                if (!File.Exists(path))
                {
                    path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "quackmenu.txt");
                }
                if (File.Exists(path))
                {
                    var cfg = new QuackConfig();
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
                            case "creativemode": cfg.CreativeModeEnabled = value; break;
                            case "flatworld": cfg.FlatWorld = value; break;
                            case "spawnheight": cfg.SpawnHeight = value; break;
                            case "openmenukey": cfg.OpenMenuKey = value; break;
                            case "bosses": cfg.Bosses = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries); break;
                            case "bossweights": cfg.BossWeights = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries); break;
                        }
                    }
                    return cfg;
                }
            }
            catch (Exception ex)
            {
                QuackMenuEntry.Log("Config load failed: " + ex.Message);
            }
            return new QuackConfig();
        }
    }
}
