using Microsoft.Xna.Framework;

namespace QuackMenu
{
    public class QuackMenuHook : GameComponent
    {
        public QuackMenuHook(Game game)
            : base(game)
        {
            UpdateOrder = int.MinValue;
        }

        public override void Update(GameTime gameTime)
        {
            QuackMenuEntry.Update();
            base.Update(gameTime);
        }
    }
}
