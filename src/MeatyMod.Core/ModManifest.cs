using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MeatyMod.Core;

public class ModManifest
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Replaces { get; set; } = new();

    public bool IsValid()
    {
        return !Validate().Any();
    }

    public IEnumerable<string> Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            yield return "Id is required.";
        }
        else if (!_idPattern.IsMatch(Id))
        {
            yield return "Id may only contain letters, digits, underscore, dot, or hyphen (no spaces or slashes).";
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return "Name is required.";
        }

        if (string.IsNullOrWhiteSpace(Version) || !System.Version.TryParse(Version, out _))
        {
            yield return "Version must be a valid version such as 1.0.0.";
        }

        if (string.IsNullOrWhiteSpace(Author))
        {
            yield return "Author is required.";
        }

        if (Replaces is null)
        {
            yield return "Replaces must not be null.";
        }
    }

    private static readonly Regex _idPattern = new("^[A-Za-z0-9_.-]+$", RegexOptions.Compiled);
}
