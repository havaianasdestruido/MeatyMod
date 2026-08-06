using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MeatyMod.Core;

namespace MeatyMod.Tests;

public class ManifestValidationTests
{
    [Fact]
    public void ValidManifest_IsValid()
    {
        var manifest = new ModManifest
        {
            Id = "oink",
            Name = "Oink",
            Version = "1.0.0",
            Author = "MeatyMod",
            Description = "Turn the player into a pig.",
            Replaces = new List<string>()
        };

        Assert.True(manifest.IsValid());
        Assert.Empty(manifest.Validate());
    }

    [Fact]
    public void MissingId_IsInvalid()
    {
        var manifest = new ModManifest { Id = string.Empty };

        Assert.False(manifest.IsValid());
        Assert.Contains(manifest.Validate(), error => error.Contains("Id", StringComparison.Ordinal));
    }

    [Fact]
    public void IdWithSpaceOrSlash_IsInvalid()
    {
        var manifest = new ModManifest { Id = "my mod/one" };

        Assert.False(manifest.IsValid());
        Assert.Contains(manifest.Validate(), error => error.Contains("Id", StringComparison.Ordinal));
    }

    [Fact]
    public void BadVersion_IsInvalid()
    {
        var manifest = new ModManifest { Version = "not-a-version" };

        Assert.False(manifest.IsValid());
        Assert.Contains(manifest.Validate(), error => error.Contains("Version", StringComparison.Ordinal));
    }

    [Fact]
    public void WhitespaceName_IsInvalid()
    {
        var manifest = new ModManifest { Name = "   " };

        Assert.False(manifest.IsValid());
        Assert.Contains(manifest.Validate(), error => error.Contains("Name", StringComparison.Ordinal));
    }

    [Fact]
    public void MissingAuthor_IsInvalid()
    {
        var manifest = new ModManifest { Author = string.Empty };

        Assert.False(manifest.IsValid());
        Assert.Contains(manifest.Validate(), error => error.Contains("Author", StringComparison.Ordinal));
    }

    [Fact]
    public void EmptyDescription_IsValid()
    {
        var manifest = new ModManifest
        {
            Id = "oink",
            Name = "Oink",
            Version = "1.0.0",
            Author = "MeatyMod",
            Description = string.Empty,
            Replaces = new List<string>()
        };

        Assert.True(manifest.IsValid());
    }

    [Fact]
    public void NullReplaces_IsInvalid()
    {
        var manifest = new ModManifest
        {
            Id = "oink",
            Name = "Oink",
            Version = "1.0.0",
            Author = "MeatyMod",
            Replaces = null!
        };

        Assert.False(manifest.IsValid());
        Assert.Contains(manifest.Validate(), error => error.Contains("Replaces", StringComparison.Ordinal));
    }

    [Fact]
    public void EmptyReplaces_IsValid()
    {
        var manifest = new ModManifest
        {
            Id = "oink",
            Name = "Oink",
            Version = "1.0.0",
            Author = "MeatyMod",
            Replaces = new List<string>()
        };

        Assert.True(manifest.IsValid());
    }

    [Fact]
    public void PopulatedReplaces_IsValid()
    {
        var manifest = new ModManifest
        {
            Id = "oink",
            Name = "Oink",
            Version = "1.0.0",
            Author = "MeatyMod",
            Replaces = new List<string> { "base-mod" }
        };

        Assert.True(manifest.IsValid());
    }

    [Fact]
    public void LoadWithValidation_MissingFile_ReturnsNullAndError()
    {
        var path = Path.Combine(Path.GetTempPath(), "missing_manifest_" + Guid.NewGuid() + ".json");

        var result = ModManifestLoader.LoadWithValidation(path);

        Assert.Null(result.Manifest);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, error => error.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void LoadWithValidation_InvalidJson_ReturnsNullAndError()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "not json");

            var result = ModManifestLoader.LoadWithValidation(path);

            Assert.Null(result.Manifest);
            Assert.NotEmpty(result.Errors);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadWithValidation_ValidJson_ReturnsManifestWithNoErrors()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "{\"Id\":\"oink\",\"Name\":\"Oink\",\"Version\":\"1.0.0\",\"Author\":\"MeatyMod\",\"Description\":\"d\",\"Replaces\":[]}");

            var result = ModManifestLoader.LoadWithValidation(path);

            Assert.NotNull(result.Manifest);
            Assert.Equal("oink", result.Manifest!.Id);
            Assert.Empty(result.Errors);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadWithValidation_BadManifest_ReturnsManifestWithErrors()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "{\"Id\":\"\",\"Name\":\"Oink\",\"Version\":\"1.0.0\",\"Author\":\"MeatyMod\"}");

            var result = ModManifestLoader.LoadWithValidation(path);

            Assert.NotNull(result.Manifest);
            Assert.NotEmpty(result.Errors);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
