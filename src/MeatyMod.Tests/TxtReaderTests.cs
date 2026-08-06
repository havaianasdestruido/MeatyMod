using System.IO;
using MeatyMod.Formats;

namespace MeatyMod.Tests;

public class TxtReaderTests
{
    [Fact]
    public void Parse_SkipsBlankLines_Trims()
    {
        var doc = TxtReader.Parse(new[] { " am ", "", " 42 ", "   ", "1.5" });

        Assert.Equal(new[] { "am", "42", "1.5" }, doc.Lines);
        Assert.Equal(3, doc.Count);
    }

    [Fact]
    public void GetInt_ParsesInvariant()
    {
        var doc = TxtReader.Parse(new[] { "42" });

        Assert.Equal(42, doc.GetInt(0));
    }

    [Fact]
    public void GetInt_OutOfRange_ReturnsZero()
    {
        var doc = TxtReader.Parse(new[] { "42" });

        Assert.Equal(0, doc.GetInt(99));
    }

    [Fact]
    public void GetFloat_ParsesDecimals()
    {
        var doc = TxtReader.Parse(new[] { "1.5" });

        Assert.Equal(1.5f, doc.GetFloat(0));
    }

    [Fact]
    public void TryGetInt_BadValue_False()
    {
        var doc = TxtReader.Parse(new[] { "abc" });

        Assert.False(doc.TryGetInt(0, out _));
    }

    [Fact]
    public void Read_MissingFile_Throws()
    {
        Assert.ThrowsAny<IOException>(() => TxtReader.Read(Path.Combine(Path.GetTempPath(), "meaty_mod_does_not_exist_xyz.txt")));
    }
}

public class CameraTrackParserTests
{
    [Fact]
    public void Parse_GroupsSixFloats()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(path, new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" });

            var result = CameraTrackParser.Parse(path);

            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].PosX);
            Assert.Equal(2, result[0].PosY);
            Assert.Equal(3, result[0].PosZ);
            Assert.Equal(4, result[0].TargetX);
            Assert.Equal(5, result[0].TargetY);
            Assert.Equal(6, result[0].TargetZ);
            Assert.Equal(9, result[1].PosZ);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Parse_DropsPartialGroup()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(path, new[] { "1", "2", "3", "4", "5", "6", "7" });

            var result = CameraTrackParser.Parse(path);

            Assert.Single(result);
        }
        finally
        {
            File.Delete(path);
        }
    }
}

public class DialogueParserTests
{
    [Fact]
    public void Parse_ReturnsNonEmptyLines()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(path, new[] { "hello", "", "world" });

            var result = DialogueParser.Parse(path);

            Assert.Equal(new[] { "hello", "world" }, result);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
