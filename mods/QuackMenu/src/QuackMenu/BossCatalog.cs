using System;

namespace QuackMenu
{
    public class BossDefinition
    {
        public string Name { get; set; }
        public string ClassName { get; set; }
        public string ModelPrefix { get; set; }
        public int Weight { get; set; }
    }

    public static class BossCatalog
    {
        public static BossDefinition[] All = new[]
        {
            new BossDefinition { Name = "Cutty",    ClassName = "Blood.Cutty4",      ModelPrefix = "cutty_",    Weight = 0 },
            new BossDefinition { Name = "Princess", ClassName = "Blood.Princess4",   ModelPrefix = "princess_", Weight = 1 },
            new BossDefinition { Name = "BoarKing", ClassName = "Blood.boarDupe6",   ModelPrefix = "boar",      Weight = 2 },
            new BossDefinition { Name = "Twin",     ClassName = "Blood.Twin",        ModelPrefix = "twin_",     Weight = 3 },
        };

        public static BossDefinition[] Enabled(QuackConfig config)
        {
            var result = new System.Collections.Generic.List<BossDefinition>();
            if (config.Bosses == null)
            {
                return result.ToArray();
            }
            foreach (var name in config.Bosses)
            {
                foreach (var def in All)
                {
                    if (string.Equals(def.Name, name.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(def);
                        break;
                    }
                }
            }
            return result.ToArray();
        }
    }
}
