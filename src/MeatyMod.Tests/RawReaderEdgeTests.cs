using System.IO;
using MeatyMod.Formats;

namespace MeatyMod.Tests;

public class RawReaderEdgeTests
{
    [Fact]
    public void TryGuessDimensions_PrefersSmallerFirst_For1024x2048()
    {
        // A 1024x2048 raw (2,097,152 samples) is ambiguous with 512x4096 (same byte count).
        // The guesser's first-divisor loop picks 512 first, so today it is misread.
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, new byte[1024 * 2048 * 2]);

            bool ok = RawReader.TryGuessDimensions(path, out int width, out int height);

            Assert.True(ok);
            Assert.Equal(512, width);
            Assert.Equal(4096, height);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void TryGuessDimensions_2000x2000_ReturnsSquare()
    {
        // The real earth.raw case: 8,000,000 bytes -> 4,000,000 samples -> 2000x2000.
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, new byte[2000 * 2000 * 2]);

            bool ok = RawReader.TryGuessDimensions(path, out int width, out int height);

            Assert.True(ok);
            Assert.Equal(2000, width);
            Assert.Equal(2000, height);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Read_ExplicitDimensions_Reads1024x2048()
    {
        // ParseCommand takes no size argument and guesses via TryGuessDimensions;
        // for a non-square raw the explicit-width/height path is the unambiguous route.
        var path = Path.GetTempFileName();
        try
        {
            var bytes = new byte[1024 * 2048 * 2];
            for (int i = 0; i < 16; i++)
            {
                ushort value = (ushort)(i + 1);
                bytes[i * 2] = (byte)(value & 0xFF);
                bytes[i * 2 + 1] = (byte)(value >> 8);
            }
            File.WriteAllBytes(path, bytes);

            var map = RawReader.Read(path, 1024, 2048);

            Assert.Equal(1024, map.Width);
            Assert.Equal(2048, map.Height);
            Assert.Equal(1024 * 2048, map.Heights.Length);
            for (int i = 0; i < 16; i++)
            {
                Assert.Equal((ushort)(i + 1), map.Heights[i]);
            }
        }
        finally
        {
            File.Delete(path);
        }
    }
}
