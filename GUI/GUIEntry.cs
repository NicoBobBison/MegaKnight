﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MegaKnight.GUI
{
    /// <summary>
    /// Run during GUI configuration.
    /// </summary>
    public class GUIEntry : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        public readonly Vector2 ScreenSize = new Vector2(1920, 1080);
        private readonly Color _backgroundColor = new Color(48, 48, 48);

        private BoardRenderer _boardRenderer;

        public GUIEntry()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = (int)ScreenSize.X;
            _graphics.PreferredBackBufferHeight = (int)ScreenSize.Y;
            _graphics.IsFullScreen = false;
            _graphics.ApplyChanges();

            base.Initialize();
        }

        protected override async void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _boardRenderer = new BoardRenderer(Content, ScreenSize);
            await _boardRenderer.TryMakeWhiteFirstMove();
        }

        protected override async void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            await _boardRenderer.Update(gameTime);
            InputManager.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(_backgroundColor);

            _spriteBatch.Begin();
            _boardRenderer.Draw(_spriteBatch);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
