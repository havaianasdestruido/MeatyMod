using System.IO;
using MeatyMod.Core;

namespace MeatyMod.Tests;

public class FileSizeGuardTests
{
    [Fact]
    public void FileSizeGuard_AllowsUnderLimit()
    {
        Assert.True(FileSizeGuard.IsAllowed(100));
        Assert.True(FileSizeGuard.IsAllowed(1024, 2048));
    }

    [Fact]
    public void FileSizeGuard_BlocksOverLimit()
    {
        Assert.False(FileSizeGuard.IsAllowed(2048, 1024));
        Assert.False(FileSizeGuard.IsAllowed(FileSizeGuard.DefaultMaxBytes + 1));
        Assert.True(FileSizeGuard.IsAllowed(FileSizeGuard.DefaultMaxBytes));
    }
}

public class ModManifestLoaderTests
{
    [Fact]
    public void ModManifestLoader_LoadsValidJson()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "{\"Id\":\"oink\",\"Name\":\"Oink\",\"Version\":\"1.0.0\",\"Author\":\"MeatyMod\",\"Description\":\"d\",\"Replaces\":[]}");

            var manifest = ModManifestLoader.Load(path);

            Assert.NotNull(manifest);
            Assert.Equal("oink", manifest.Id);
            Assert.Equal("1.0.0", manifest.Version);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ModManifestLoader_MissingFile_Null()
    {
        var manifest = ModManifestLoader.Load(Path.Combine(Path.GetTempPath(), "meaty_mod_missing_manifest_xyz.json"));

        Assert.Null(manifest);
    }

    [Fact]
    public void ModManifestLoader_InvalidJson_Null()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "not json");

            var manifest = ModManifestLoader.Load(path);

            Assert.Null(manifest);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
