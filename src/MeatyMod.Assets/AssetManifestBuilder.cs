using System.Collections.Generic;
using System.IO;

namespace MeatyMod.Assets
{
    public static class AssetManifestBuilder
    {
        public static IDictionary<string, string> Build(string gameContentPath)
        {
            var manifest = new Dictionary<string, string>();

            foreach (var file in Directory.EnumerateFiles(gameContentPath, "*.xnb", SearchOption.AllDirectories))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                manifest[name] = file;
            }

            return manifest;
        }
    }
}
