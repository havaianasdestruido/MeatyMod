using System;
using System.IO;
using System.Security.Cryptography;
using MeatyMod.Formats;

namespace MeatyMod.Tests;

public class LzxFixtureTests
{
    private const string FixtureFileName = "darkFog3_0.xnb";
    private const int ExpectedDecompressedSize = 893;
    private const string ExpectedSha256 = "FB2F56A61B40ADB076C09B27128494ADF106C511B6C778191909D8A9248E2692";

    private static string FixturePath =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", FixtureFileName);

    [Fact]
    public void Read_FixtureIsCompressedLzx_AndDecompressedSizeMatchesHeader()
    {
        var path = FixturePath;
        Assert.True(File.Exists(path), $"Fixture missing: {path}");

        var content = XnbContentReader.Read(path);

        Assert.Equal("XNB", content.Magic);
        Assert.Equal(5, content.Version);
        Assert.Equal(0x77, content.Platform);
        Assert.Equal(0x81, content.Flags);
        Assert.True(content.IsCompressed);
        Assert.Equal(ExpectedDecompressedSize, content.DecompressedSize);
        Assert.Equal(ExpectedDecompressedSize, content.Content.Length);
    }

    [Fact]
    public void Read_Fixture_DecompressedBytesMatchKnownSha256()
    {
        var path = FixturePath;
        Assert.True(File.Exists(path), $"Fixture missing: {path}");

        var content = XnbContentReader.Read(path);
        var hash = Convert.ToHexString(SHA256.HashData(content.Content));

        Assert.Equal(ExpectedSha256, hash);
    }
}
