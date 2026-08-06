using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Oink
{
    public static class OinkSkin
    {
        private static Texture2D _pig;
        private static object _original;
        private static bool _stashed;
        private static object _originalOrig;
        private static bool _origStashed;

        public static void Apply(Game game, object gameScreen, OinkConfig config)
        {
            if (gameScreen == null)
            {
                return;
            }

            if (_pig == null)
            {
                try
                {
                    _pig = game.Content.Load<Texture2D>(config.PigTexture);
                }
                catch (Exception ex)
                {
                    OinkEntry.Log("Pig texture load failed: " + ex.Message);
                    return;
                }
            }

            object current = OinkReflect.GetField(gameScreen, "player1Texture");
            if (!_stashed && current != null)
            {
                _original = current;
                _stashed = true;
            }

            object currentOrig = OinkReflect.GetField(gameScreen, "player1TextureOrig");
            if (!_origStashed && currentOrig != null)
            {
                _originalOrig = currentOrig;
                _origStashed = true;
            }

            OinkReflect.SetField(gameScreen, "player1Texture", _pig);
            OinkReflect.SetField(gameScreen, "player1TextureOrig", _pig);
        }

        public static void Restore(object gameScreen)
        {
            if (gameScreen == null || !_stashed)
            {
                return;
            }

            OinkReflect.SetField(gameScreen, "player1Texture", _original);
            if (_origStashed)
            {
                OinkReflect.SetField(gameScreen, "player1TextureOrig", _originalOrig);
            }

            _stashed = false;
            _origStashed = false;
        }
    }
}
