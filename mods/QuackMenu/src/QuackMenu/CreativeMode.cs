using System;
using System.Globalization;

namespace QuackMenu
{
    public static class CreativeMode
    {
        public static void Apply(object screenManager, QuackConfig config)
        {
            if (screenManager == null)
            {
                return;
            }

            bool enabled = ParseBool(config.CreativeModeEnabled, true);
            bool flat = ParseBool(config.FlatWorld, true);

            if (!enabled)
            {
                return;
            }

            SetField(screenManager, "hostAllowCheats", true);
            SetField(screenManager, "myplayerCheats", true);
            SetField(screenManager, "allWeapons", true);
            SetField(screenManager, "developer", true);

            if (flat)
            {
                SetField(screenManager, "curDay", 1);
                SetField(screenManager, "currentDay", 1);
                SetField(screenManager, "tempcurrentDay", 1);

                float spawnY = ParseFloat(config.SpawnHeight, 3f);
                SetField(screenManager, "spawnY", spawnY);
            }

            QuackMenuEntry.Log("Creative mode applied (enabled=" + enabled + ", flat=" + flat + ").");
        }

        private static void SetField(object target, string name, object value)
        {
            try
            {
                var field = target.GetType().GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(target, value);
                }
                else
                {
                    QuackMenuEntry.Log("Field not found: " + name);
                }
            }
            catch (Exception ex)
            {
                QuackMenuEntry.Log("Failed to set " + name + ": " + ex.Message);
            }
        }

        private static bool ParseBool(string s, bool fallback)
        {
            bool r;
            return bool.TryParse(s, out r) ? r : fallback;
        }

        private static float ParseFloat(string s, float fallback)
        {
            float r;
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out r) ? r : fallback;
        }
    }
}
