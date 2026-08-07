using System.IO;
using System.Text;
using MeatyMod.Cli.Commands;

namespace MeatyMod.Tests;

public class VerifyCommandTests
{
    private static string NewTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "meatymod_verify_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static byte[] ValidUncompressedXnb()
    {
        var bytes = new List<byte>();
        bytes.AddRange(Encoding.ASCII.GetBytes("XNB"));
        bytes.Add(0x77);
        bytes.Add(5);
        bytes.Add(1);
        bytes.AddRange(BitConverter.GetBytes(14));
        return bytes.ToArray();
    }

    private static (int ExitCode, string Stdout, string Stderr) Run(string path)
    {
        var originalOut = Console.Out;
        var originalErr = Console.Error;
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        try
        {
            Console.SetOut(stdout);
            Console.SetError(stderr);
            int exitCode = new VerifyCommand().Run(new[] { path });
            return (exitCode, stdout.ToString(), stderr.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
        }
    }

    [Fact]
    public void Verify_EmptyDirectory_ReturnsFailure()
    {
        var dir = NewTempDir();
        try
        {
            var result = Run(dir);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("No XNB files found", result.Stderr);
        }
        finally
        {
            Directory.Delete(dir);
        }
    }

    [Fact]
    public void Verify_DirectoryWithValidUncompressedXnb_ReturnsSuccess()
    {
        var dir = NewTempDir();
        try
        {
            File.WriteAllBytes(Path.Combine(dir, "valid.xnb"), ValidUncompressedXnb());

            var result = Run(dir);

            Assert.Equal(0, result.ExitCode);
            Assert.Contains("Valid: 1 Invalid: 0 Total: 1", result.Stdout);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
