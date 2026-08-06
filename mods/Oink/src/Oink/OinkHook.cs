using Microsoft.Xna.Framework;

namespace Oink
{
    public class OinkHook : GameComponent
    {
        public OinkHook(Game game)
            : base(game)
        {
            UpdateOrder = int.MaxValue;
        }

        public override void Update(GameTime gameTime)
        {
            if (!_loggedOnce)
            {
                _loggedOnce = true;
                OinkEntry.Log("Hook update fired (first call). ScreenManager=" + (OinkEntry.ScreenManager == null ? "null" : OinkEntry.ScreenManager.GetType().FullName));
            }
            OinkEntry.Update();
            base.Update(gameTime);
        }

        private static bool _loggedOnce;
    }
}
