using System.Security.Cryptography;

namespace MeatyMod.Core;

public static class ChecksumUtil
{
    public static string Sha256File(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    public static string Sha256(byte[] data)
    {
        return Convert.ToHexStringLower(SHA256.HashData(data));
    }
}
