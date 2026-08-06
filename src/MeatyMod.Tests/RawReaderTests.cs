using System;
using System.IO;
using MeatyMod.Formats;

namespace MeatyMod.Tests;

public class RawReaderTests
{
    [Fact]
    public void Read_ParsesSequentialValues()
    {
        var path = Path.GetTempFileName();
        try
        {
            var bytes = new byte[4 * 4 * 2];
            for (int i = 0; i < 16; i++)
            {
                ushort value = (ushort)(i + 1);
                bytes[i * 2] = (byte)(value & 0xFF);
                bytes[i * 2 + 1] = (byte)(value >> 8);
            }
            File.WriteAllBytes(path, bytes);

            var map = RawReader.Read(path, 4, 4);

            Assert.Equal(4, map.Width);
            Assert.Equal(4, map.Height);
            Assert.Equal(16, map.Heights.Length);
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

    [Fact]
    public void Read_InterpretsLittleEndian()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, new byte[] { 0x02, 0x01 });

            var map = RawReader.Read(path, 1, 1);

            Assert.Equal((ushort)0x0102, map.Heights[0]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Read_TooShortFile_Throws()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, new byte[4]);

            Assert.ThrowsAny<IOException>(() => RawReader.Read(path, 4, 4));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void TryGuessDimensions_2048x2048_ReturnsSquare()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, new byte[2048 * 2048 * 2]);

            bool ok = RawReader.TryGuessDimensions(path, out int width, out int height);

            Assert.True(ok);
            Assert.Equal(2048, width);
            Assert.Equal(2048, height);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void TryGuessDimensions_OddLength_False()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, new byte[5]);

            Assert.False(RawReader.TryGuessDimensions(path, out _, out _));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
