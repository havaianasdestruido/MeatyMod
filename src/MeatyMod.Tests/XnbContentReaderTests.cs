using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MeatyMod.Formats;

namespace MeatyMod.Tests;

public class XnbContentReaderTests
{
    private const string RealGameFile = @"C:\Users\mcmco\Desktop\MeatyMod\game\Blood and Bacon\Content\armadillo2_0.xnb";

    private static string WriteTempFile(byte[] bytes)
    {
        var path = Path.Combine(Path.GetTempPath(), "xnb_" + Guid.NewGuid().ToString("N") + ".xnb");
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private static List<byte> Header(byte platform, byte version, byte flags, int xnbLength)
    {
        var bytes = new List<byte>();
        bytes.AddRange(Encoding.ASCII.GetBytes("XNB"));
        bytes.Add(platform);
        bytes.Add(version);
        bytes.Add(flags);
        bytes.AddRange(BitConverter.GetBytes(xnbLength));
        return bytes;
    }

    [Fact]
    public void Read_CraftedCompressedHeader_ParsesFields()
    {
        var bytes = Header(0x77, 5, 0x80, 14);
        bytes.AddRange(BitConverter.GetBytes(0));
        var path = WriteTempFile(bytes.ToArray());
        try
        {
            var content = XnbContentReader.Read(path);

            Assert.Equal("XNB", content.Magic);
            Assert.Equal(0x77, content.Platform);
            Assert.Equal(5, content.Version);
            Assert.Equal(0x80, content.Flags);
            Assert.True(content.IsCompressed);
            Assert.Equal(0, content.DecompressedSize);
            Assert.Empty(content.Content);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Read_Uncompressed_ContentEqualsPayload()
    {
        var payload = new byte[] { 1, 2, 3, 4, 5, 200, 250 };
        var bytes = Header(0x77, 5, 0x00, 10 + payload.Length);
        bytes.AddRange(payload);
        var path = WriteTempFile(bytes.ToArray());
        try
        {
            var content = XnbContentReader.Read(path);

            Assert.Equal("XNB", content.Magic);
            Assert.Equal(0x77, content.Platform);
            Assert.Equal(5, content.Version);
            Assert.Equal(0x00, content.Flags);
            Assert.False(content.IsCompressed);
            Assert.Equal(payload.Length, content.DecompressedSize);
            Assert.Equal(payload, content.Content);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Read_RealGameFile_ReturnsVersion5AndPlatformW()
    {
        var path = ResolveRealGameFile();
        var content = XnbContentReader.Read(path);

        Assert.Equal(5, content.Version);
        Assert.Equal(0x77, content.Platform);
        Assert.True(content.Content.Length > 0);
    }

    [Fact]
    public void Read_InvalidMagic_Throws()
    {
        var path = WriteTempFile(Encoding.ASCII.GetBytes("PNGxxxx"));
        try
        {
            Assert.Throws<InvalidDataException>(() => XnbContentReader.Read(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Read_UnsupportedVersion_Throws()
    {
        var bytes = Header(0x77, 3, 0x00, 10);
        var path = WriteTempFile(bytes.ToArray());
        try
        {
            Assert.Throws<InvalidDataException>(() => XnbContentReader.Read(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string ResolveRealGameFile()
    {
        if (File.Exists(RealGameFile)) return RealGameFile;
        var relative = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "game", "Blood and Bacon", "Content", "armadillo2_0.xnb");
        return Path.GetFullPath(relative);
    }
}
