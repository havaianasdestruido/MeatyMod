using System;
using System.IO;

namespace MeatyMod.Formats
{
    public static class XnbReader
    {
        public static XnbHeader ReadHeader(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
            var magic = new string(reader.ReadChars(3));
            if (magic != "XNB") throw new InvalidDataException("Not XNB file.");
            var version = reader.ReadByte();
            var flags = reader.ReadByte();
            return new XnbHeader { Magic = magic, Version = version, Flags = flags };
        }
    }

    public class XnbHeader
    {
        public string Magic { get; set; }
        public byte Version { get; set; }
        public byte Flags { get; set; }
    }
}
