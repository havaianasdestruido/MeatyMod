using System.IO;
using System.Security.Cryptography;
using System.Text;
using MeatyMod.Core;

namespace MeatyMod.Tests;

public class ChecksumUtilTests
{
    [Fact]
    public void Sha256_OfAbc_MatchesKnownHash()
    {
        var data = Encoding.UTF8.GetBytes("abc");
        const string expected = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";

        var actual = ChecksumUtil.Sha256(data);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Sha256File_MatchesDirectFileStreamHash()
    {
        var path = Path.Combine(Path.GetTempPath(), $"meatymod_checksum_{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllBytes(path, Encoding.UTF8.GetBytes("hello meatymod checksum"));

            string expected;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                expected = Convert.ToHexStringLower(SHA256.HashData(stream));
            }

            var actual = ChecksumUtil.Sha256File(path);

            Assert.Equal(expected, actual);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
