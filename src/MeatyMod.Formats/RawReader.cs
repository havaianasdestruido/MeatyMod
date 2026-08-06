using System;
using System.IO;

namespace MeatyMod.Formats;

public sealed class RawHeightmap
{
    public int Width;
    public int Height;
    public ushort[] Heights = [];
}

public static class RawReader
{
    public static RawHeightmap Read(string path, int width = 2048, int height = 2048)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width and height must be positive.");
        }

        var requiredBytes = (long)width * height * 2;
        var info = new FileInfo(path);
        if (info.Length < requiredBytes)
        {
            throw new IOException($"File is too short for a {width}x{height} heightmap: {info.Length} bytes, expected at least {requiredBytes}.");
        }

        var heights = new ushort[width * height];
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new BinaryReader(stream);
        for (var i = 0; i < heights.Length; i++)
        {
            heights[i] = reader.ReadUInt16();
        }
        return new RawHeightmap { Width = width, Height = height, Heights = heights };
    }

    public static bool TryGuessDimensions(string path, out int width, out int height)
    {
        width = 0;
        height = 0;

        var length = new FileInfo(path).Length;
        if (length < 2 || length % 2 != 0)
        {
            return false;
        }

        var samples = length / 2;
        int[] sizes = [512, 1024, 2048, 4096];

        foreach (var size in sizes)
        {
            if (samples == (long)size * size)
            {
                width = size;
                height = size;
                return true;
            }
        }

        foreach (var size in sizes)
        {
            if (samples % size != 0)
            {
                continue;
            }

            var other = samples / size;
            if (other is > 0 and <= int.MaxValue)
            {
                width = size;
                height = (int)other;
                return true;
            }
        }

        return false;
    }
}
