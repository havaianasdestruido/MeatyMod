using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Oink
{
    public static class OinkEntry
    {
        private static bool _injected;
        private static Game _game;
        private static object _screenManager;
        private static OinkConfig _config;
        private static OinkHook _hook;
        private static bool _enabled = true;
        private static Dictionary<Keys, bool> _lastKeys = new Dictionary<Keys, bool>();

        public static void Inject(Game game)
        {
            if (_injected)
            {
                return;
            }

            _game = game;
            _config = OinkConfig.Load();
            FindScreenManager(game);

            if (_screenManager != null && ParseBool(_config.Enabled, true))
            {
                _enabled = true;
            }

            if (!game.Components.Contains(_hook))
            {
                _hook = new OinkHook(game);
                game.Components.Add(_hook);
            }

            _injected = true;
            Log("Oink injected.");
        }

        public static Game Game => _game;
        public static object ScreenManager => _screenManager;
        public static OinkConfig Config => _config;
        public static bool Enabled => _enabled;

        public static void Update()
        {
            if (!_injected || _config == null)
            {
                return;
            }

            Keys toggleKey = ParseKey(_config.ToggleKey, Keys.O);
            bool now = IsKeyDown(toggleKey);
            bool was = _lastKeys.TryGetValue(toggleKey, out bool w) && w;
            _lastKeys[toggleKey] = now;

            if (now && !was)
            {
                SetEnabled(!_enabled);
            }

            if (_screenManager == null)
            {
                FindScreenManager(_game);
            }

            object gameScreen = _screenManager == null ? null : OinkReflect.FindGameScreen(_screenManager);
            if (_enabled)
            {
                if (ParseBool(_config.PigSkin, true))
                {
                    OinkSkin.Apply(_game, gameScreen, _config);
                }
                OinkSpeed.Apply(gameScreen, ParseFloat(_config.SpeedMultiplier, 1.35f));
            }
            else
            {
                OinkSkin.Restore(gameScreen);
            }
        }

        public static void SetEnabled(bool value)
        {
            _enabled = value;
            Log("Oink " + (value ? "enabled." : "disabled."));
        }

        private static void FindScreenManager(Game game)
        {
            try
            {
                foreach (var c in game.Components)
                {
                    if (c != null && c.GetType().FullName == "Blood.ScreenManager")
                    {
                        _screenManager = c;
                        return;
                    }
                }
                Log("ScreenManager not found in Components.");
            }
            catch (Exception ex)
            {
                Log("FindScreenManager failed: " + ex.Message);
            }
        }

        private static bool IsKeyDown(Keys key)
        {
            try
            {
                return Keyboard.GetState().IsKeyDown(key);
            }
            catch
            {
                return false;
            }
        }

        private static Keys ParseKey(string s, Keys fallback)
        {
            try
            {
                return (Keys)Enum.Parse(typeof(Keys), s, ignoreCase: true);
            }
            catch
            {
                return fallback;
            }
        }

        private static bool ParseBool(string s, bool fallback)
        {
            bool value;
            if (bool.TryParse(s, out value))
            {
                return value;
            }
            return fallback;
        }

        private static float ParseFloat(string s, float fallback)
        {
            float value;
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return value;
            }
            return fallback;
        }

        public static void Log(string message)
        {
            try
            {
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                File.AppendAllText(Path.Combine(dir, "oink.log"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
