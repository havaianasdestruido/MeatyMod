using System;
using System.IO;
using System.Text.Json;

namespace MeatyMod.Core
{
    public static class ModManifestLoader
    {
        public static ModManifest? Load(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                var json = File.ReadAllText(path);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<ModManifest>(json, options);
            }
            catch
            {
                return null;
            }
        }
    }
}
