using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MeatyMod.Core;

public static class ModManifestLoader
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static ModManifest? Load(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ModManifest>(json, _options);
        }
        catch
        {
            return null;
        }
    }

    public static (ModManifest? Manifest, string[] Errors) LoadWithValidation(string path)
    {
        if (!File.Exists(path))
        {
            return (null, [$"Manifest file not found: {path}"]);
        }

        try
        {
            var json = File.ReadAllText(path);
            var manifest = JsonSerializer.Deserialize<ModManifest>(json, _options);
            if (manifest is null)
            {
                return (null, ["Manifest could not be deserialized."]);
            }

            return (manifest, manifest.Validate().ToArray());
        }
        catch (JsonException ex)
        {
            return (null, [$"Invalid manifest JSON: {ex.Message}"]);
        }
        catch (IOException ex)
        {
            return (null, [$"Could not read manifest: {ex.Message}"]);
        }
    }
}
