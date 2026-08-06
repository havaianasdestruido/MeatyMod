using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace MeatyMod.Formats
{
    public sealed class TxtDocument
    {
        private readonly IReadOnlyList<string> _lines;

        public TxtDocument(IReadOnlyList<string> lines)
        {
            _lines = lines;
        }

        public IReadOnlyList<string> Lines => _lines;

        public int Count => _lines.Count;

        public string GetString(int index)
        {
            if (index < 0 || index >= _lines.Count) return string.Empty;
            return _lines[index];
        }

        public bool GetBool(int index)
        {
            return GetInt(index) != 0;
        }

        public int GetInt(int index)
        {
            return TryGetInt(index, out var value) ? value : 0;
        }

        public float GetFloat(int index)
        {
            return TryGetFloat(index, out var value) ? value : 0f;
        }

        public bool TryGetInt(int index, out int value)
        {
            value = 0;
            if (index < 0 || index >= _lines.Count) return false;
            return int.TryParse(_lines[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        public bool TryGetFloat(int index, out float value)
        {
            value = 0f;
            if (index < 0 || index >= _lines.Count) return false;
            return float.TryParse(_lines[index], NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }

    public static class TxtReader
    {
        public static TxtDocument Read(string path)
        {
            using var reader = File.OpenText(path);
            var lines = new List<string>();
            while (reader.ReadLine() is string line)
            {
                var trimmed = line.Trim();
                if (trimmed.Length > 0) lines.Add(trimmed);
            }
            return new TxtDocument(lines);
        }

        public static TxtDocument Parse(IEnumerable<string> lines)
        {
            var result = new List<string>();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Length > 0) result.Add(trimmed);
            }
            return new TxtDocument(result);
        }
    }
}
