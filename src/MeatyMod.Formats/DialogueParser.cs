using System.Collections.Generic;
using System.IO;

namespace MeatyMod.Formats
{
    public static class DialogueParser
    {
        public static IReadOnlyList<string> Parse(string path)
        {
            var result = new List<string>();
            using var reader = File.OpenText(path);
            while (reader.ReadLine() is string line)
            {
                var trimmed = line.Trim();
                if (trimmed.Length > 0) result.Add(trimmed);
            }
            return result;
        }
    }
}
