using System;
using System.IO;

namespace MeatyMod.Formats;

public static class XnbContentReader
{
    private const int MaxDecodedFrameSize = 0x8000;

    public static XnbContent Read(string path)
    {
        if (path == null) throw new ArgumentNullException(nameof(path));
        using var stream = File.OpenRead(path);
        return Read(stream);
    }

    private static XnbContent Read(Stream stream)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        var magicBytes = reader.ReadBytes(3);
        if (magicBytes.Length != 3 || magicBytes[0] != 0x58 || magicBytes[1] != 0x4E || magicBytes[2] != 0x42)
            throw new InvalidDataException("Not XNB file.");

        var platform = reader.ReadByte();
        var version = reader.ReadByte();
        if (version != 5 && version != 4) throw new InvalidDataException($"Unsupported XNB version {version}.");
        var flags = reader.ReadByte();

        var xnbLength = reader.ReadInt32();
        if (xnbLength < 0) throw new InvalidDataException("Invalid XNB file length.");

        if ((flags & 0x80) != 0)
        {
            var decompressedSize = reader.ReadInt32();
            if (decompressedSize < 0) throw new InvalidDataException("Invalid XNB decompressed size.");
            var compressedSize = Math.Min(xnbLength - (int)stream.Position, (int)(stream.Length - stream.Position));
            return new XnbContent
            {
                Magic = "XNB",
                Platform = platform,
                Version = version,
                Flags = flags,
                DecompressedSize = decompressedSize,
                IsCompressed = true,
                Content = DecompressLzx(stream, decompressedSize, compressedSize),
            };
        }

        var content = reader.ReadBytes((int)(stream.Length - stream.Position));
        return new XnbContent
        {
            Magic = "XNB",
            Platform = platform,
            Version = version,
            Flags = flags,
            DecompressedSize = content.Length,
            IsCompressed = false,
            Content = content,
        };
    }

    private static byte[] DecompressLzx(Stream stream, int decompressedSize, int compressedSize)
    {
        using var output = new MemoryStream(decompressedSize);
        var decoder = new LzxDecoder(16);
        var startPos = stream.Position;
        var pos = startPos;

        while (pos - startPos < compressedSize)
        {
            var hi = stream.ReadByte();
            var lo = stream.ReadByte();
            if (hi < 0 || lo < 0) throw new InvalidDataException("Unexpected end of LZX data.");
            var blockSize = (hi << 8) | lo;
            var frameSize = MaxDecodedFrameSize;

            if (hi == 0xFF)
            {
                hi = lo;
                lo = stream.ReadByte();
                frameSize = (hi << 8) | lo;
                hi = stream.ReadByte();
                lo = stream.ReadByte();
                blockSize = (hi << 8) | lo;
                pos += 5;
            }
            else
            {
                pos += 2;
            }

            if (blockSize == 0 || frameSize == 0) break;

            if (decoder.Decompress(stream, blockSize, output, frameSize) != 0)
                throw new InvalidDataException("LZX decompression failed.");

            pos += blockSize;
            stream.Seek(pos, SeekOrigin.Begin);
        }

        if (output.Position != decompressedSize) throw new InvalidDataException("LZX decompression failed.");
        return output.ToArray();
    }
}

public sealed class XnbContent
{
    public string Magic;
    public byte Platform;
    public byte Version;
    public byte Flags;
    public int DecompressedSize;
    public bool IsCompressed;
    public byte[] Content;
}
