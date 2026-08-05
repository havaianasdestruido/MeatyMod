using System.IO;

namespace MeatyMod.Formats
{
    public class XnbReader
    {
        public void Read(BinaryReader reader)
        {
            char[] magic = reader.ReadChars(3);
            if (magic[0] != 'X' || magic[1] != 'N' || magic[2] != 'B')
                throw new InvalidDataException("Not XNB file.");
            
            byte version = reader.ReadByte();
            byte flags = reader.ReadByte();
        }
    }
}
