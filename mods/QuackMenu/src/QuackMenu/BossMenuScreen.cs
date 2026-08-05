using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace QuackMenu
{
    public class BossMenuScreen : DrawableGameComponent
    {
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;
        private Texture2D _pixel;
        private int _selected;
        private BossDefinition[] _bosses;
        private bool _active;
        private KeyboardState _prevKeyboard;

        public Action<BossDefinition> OnRequestSpawn { get; set; }
        public Action OnRequestClose { get; set; }

        public bool IsActive => _active;

        public BossMenuScreen(Game game)
            : base(game)
        {
            _bosses = BossCatalog.All;
        }

        public void Activate()
        {
            _active = true;
            _selected = 0;
            DrawOrder = int.MaxValue - 1;
            UpdateOrder = int.MaxValue - 1;
            Enabled = true;
            Visible = true;
        }

        public void Deactivate()
        {
            _active = false;
            Enabled = false;
            Visible = false;
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
            _font = TryLoadFont(Game.Content, "QuackMenu");
            if (_font == null)
            {
                _font = TryLoadFont(Game.Content, "Arial");
            }
            if (_font == null)
            {
                QuackMenuEntry.Log("No usable sprite font found; menu text will not render.");
            }
        }

        private static SpriteFont TryLoadFont(ContentManager content, string name)
        {
            try
            {
                return content.Load<SpriteFont>(name);
            }
            catch
            {
                return null;
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (!_active)
            {
                return;
            }

            KeyboardState state = Keyboard.GetState();

            if (WasPressed(state, Keys.Down))
            {
                _selected = (_selected + 1) % _bosses.Length;
            }
            else if (WasPressed(state, Keys.Up))
            {
                _selected = (_selected - 1 + _bosses.Length) % _bosses.Length;
            }

            if (WasPressed(state, Keys.Enter))
            {
                OnRequestSpawn?.Invoke(_bosses[_selected]);
            }
            else if (WasPressed(state, Keys.Escape))
            {
                OnRequestClose?.Invoke();
            }

            _prevKeyboard = state;
        }

        private bool WasPressed(KeyboardState state, Keys key)
        {
            return state.IsKeyDown(key) && !_prevKeyboard.IsKeyDown(key);
        }

        public override void Draw(GameTime gameTime)
        {
            if (!_active || _spriteBatch == null)
            {
                return;
            }

            var vp = GraphicsDevice.Viewport;
            _spriteBatch.Begin();

            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), new Color(0, 0, 0, 180));

            if (_font != null)
            {
                Vector2 titlePos = new Vector2(40, 30);
                _spriteBatch.DrawString(_font, "QUACK MENU - BOSS SPAWNER", titlePos, Color.White);

                for (int i = 0; i < _bosses.Length; i++)
                {
                    Color c = (i == _selected) ? Color.Yellow : Color.White;
                    string marker = (i == _selected) ? "> " : "  ";
                    Vector2 pos = new Vector2(40, 80 + i * 40);
                    _spriteBatch.DrawString(_font, marker + _bosses[i].Name + "  [Enter=spawn]", pos, c);
                }

                Vector2 hintPos = new Vector2(40, 80 + _bosses.Length * 40 + 20);
                _spriteBatch.DrawString(_font, "Up/Down = navigate, Enter = spawn, Esc = close", hintPos, Color.Gray);
            }

            _spriteBatch.End();
        }
    }
}
