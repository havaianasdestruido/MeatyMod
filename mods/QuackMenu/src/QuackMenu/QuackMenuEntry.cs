using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace QuackMenu
{
    public static class QuackMenuEntry
    {
        private static bool _injected;
        private static Game _game;
        private static object _screenManager;
        private static QuackConfig _config;
        private static BossMenuScreen _menu;
        private static QuackMenuHook _hook;
        private static Dictionary<Keys, bool> _lastKeys = new Dictionary<Keys, bool>();

        public static void Inject(Game game)
        {
            if (_injected)
            {
                return;
            }

            _game = game;
            _config = QuackConfig.Load();
            FindScreenManager(game);

            if (_screenManager != null)
            {
                CreativeMode.Apply(_screenManager, _config);
            }

            if (!game.Components.Contains(_hook))
            {
                _hook = new QuackMenuHook(game);
                game.Components.Add(_hook);
            }

            _injected = true;
            Log("QuackMenu injected.");
        }

        public static Game Game => _game;
        public static object ScreenManager => _screenManager;
        public static QuackConfig Config => _config;

        public static void Update()
        {
            if (!_injected || _config == null)
            {
                return;
            }

            Keys openKey = ParseKey(_config.OpenMenuKey, Keys.F1);
            bool now = IsKeyDown(openKey);
            bool was = _lastKeys.TryGetValue(openKey, out bool w) && w;
            _lastKeys[openKey] = now;

            if (now && !was)
            {
                ToggleMenu();
            }
        }

        public static void ToggleMenu()
        {
            if (_menu == null || !_menu.IsActive)
            {
                OpenMenu();
            }
            else
            {
                CloseMenu();
            }
        }

        public static void OpenMenu()
        {
            if (_game == null)
            {
                return;
            }
            if (_menu == null)
            {
                _menu = new BossMenuScreen(_game);
                _menu.OnRequestSpawn = SpawnBoss;
                _menu.OnRequestClose = CloseMenu;
            }
            if (!_game.Components.Contains(_menu))
            {
                _game.Components.Add(_menu);
            }
            _menu.Activate();
        }

        public static void CloseMenu()
        {
            if (_menu != null && _game != null)
            {
                _menu.Deactivate();
                _game.Components.Remove(_menu);
            }
        }

        public static void SpawnBoss(BossDefinition def)
        {
            if (def == null)
            {
                return;
            }
            try
            {
                BossSpawner.Spawn(_screenManager, def, _config);
                Log("Spawned boss: " + def.Name);
            }
            catch (Exception ex)
            {
                Log("Failed to spawn boss '" + def.Name + "': " + ex.Message);
            }
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

        public static void Log(string message)
        {
            try
            {
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                File.AppendAllText(Path.Combine(dir, "quackmenu.log"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
