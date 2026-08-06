using System;

namespace Oink
{
    public static class OinkSpeed
    {
        private static bool _logged;
        private static float _lastLogged;

        public static void Apply(object gameScreen, float multiplier)
        {
            if (gameScreen == null)
            {
                return;
            }

            if (multiplier <= 0f || multiplier == 1f)
            {
                return;
            }

            object myPlayer = OinkReflect.GetField(gameScreen, "myPlayer");
            if (myPlayer == null)
            {
                return;
            }

            object sprintObj = OinkReflect.GetField(myPlayer, "sprint");
            if (sprintObj == null)
            {
                return;
            }

            float baseSprint;
            try
            {
                baseSprint = (float)sprintObj;
            }
            catch
            {
                return;
            }

            float newSprint = baseSprint * multiplier;
            OinkReflect.SetField(myPlayer, "sprint", newSprint);

            if (!_logged || _lastLogged != baseSprint)
            {
                _logged = true;
                _lastLogged = baseSprint;
                OinkEntry.Log("Speed applied: " + baseSprint + " -> " + newSprint + ".");
            }
        }
    }
}
