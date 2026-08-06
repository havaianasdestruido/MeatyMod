using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace MeatyMod.Formats
{
    public struct CameraKeyframe
    {
        public float PosX, PosY, PosZ;
        public float TargetX, TargetY, TargetZ;
    }

    public static class CameraTrackParser
    {
        public static IReadOnlyList<CameraKeyframe> Parse(string path)
        {
            var result = new List<CameraKeyframe>();
            var group = new List<float>(6);
            using var reader = File.OpenText(path);
            while (reader.ReadLine() is string line)
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0) continue;
                if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    group.Add(value);
                    if (group.Count == 6)
                    {
                        result.Add(new CameraKeyframe
                        {
                            PosX = group[0],
                            PosY = group[1],
                            PosZ = group[2],
                            TargetX = group[3],
                            TargetY = group[4],
                            TargetZ = group[5]
                        });
                        group.Clear();
                    }
                }
            }
            return result;
        }
    }
}
