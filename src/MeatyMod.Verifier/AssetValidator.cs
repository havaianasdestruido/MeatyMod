using System.IO;

namespace MeatyMod.Verifier;

public static class AssetValidator
{
    public static bool ValidateXnb(string path)
    {
        byte[] header = new byte[5];
        using (FileStream stream = File.OpenRead(path))
        {
            int read = stream.Read(header, 0, header.Length);
            if (read < header.Length)
            {
                return false;
            }
        }

        return header[0] == 0x58 && header[1] == 0x4E && header[2] == 0x42 && header[3] == 0x77 && header[4] == 5;
    }

    public static (int Valid, int Invalid, int Total) ValidateDirectory(string dir)
    {
        int valid = 0;
        int invalid = 0;

        foreach (string file in Directory.EnumerateFiles(dir, "*.xnb", SearchOption.AllDirectories))
        {
            try
            {
                if (ValidateXnb(file))
                {
                    valid++;
                }
                else
                {
                    invalid++;
                }
            }
            catch (IOException)
            {
                invalid++;
            }
        }

        return (valid, invalid, valid + invalid);
    }
}
