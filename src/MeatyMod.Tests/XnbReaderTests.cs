using System.IO;
using System.Text;
using MeatyMod.Formats;

namespace MeatyMod.Tests;

public class XnbReaderTests
{
    [Fact]
    public void ValidHeader_ReturnsMagic()
    {
        using var stream = new MemoryStream();
        var bytes = Encoding.UTF8.GetBytes("XNB");
        stream.Write(bytes, 0, bytes.Length);
        stream.WriteByte(5);
        stream.WriteByte(1);
        stream.Position = 0;

        var header = XnbReader.ReadHeader(stream);

        Assert.Equal("XNB", header.Magic);
    }

    [Fact]
    public void ValidHeader_ReturnsVersion()
    {
        using var stream = new MemoryStream();
        var bytes = Encoding.UTF8.GetBytes("XNB");
        stream.Write(bytes, 0, bytes.Length);
        stream.WriteByte(5);
        stream.WriteByte(1);
        stream.Position = 0;

        var header = XnbReader.ReadHeader(stream);

        Assert.Equal(5, header.Version);
    }

    [Fact]
    public void InvalidMagic_Throws()
    {
        using var stream = new MemoryStream();
        var bytes = Encoding.UTF8.GetBytes("PNG");
        stream.Write(bytes, 0, bytes.Length);
        stream.Position = 0;

        Assert.Throws<InvalidDataException>(() => XnbReader.ReadHeader(stream));
    }
}
