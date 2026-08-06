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
            OinkEntry.Update();
            base.Update(gameTime);
        }
    }
}
