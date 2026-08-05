using System.IO;

namespace MeatyMod.Verifier;

public static class AssetValidator
{
    public static bool ValidateXnb(string path)
    {
        byte[] header = new byte[3];
        using (FileStream stream = File.OpenRead(path))
        {
            int read = stream.Read(header, 0, header.Length);
            if (read < header.Length)
            {
                return false;
            }
        }

        return header[0] == 0x58 && header[1] == 0x4E && header[2] == 0x42;
    }
}
