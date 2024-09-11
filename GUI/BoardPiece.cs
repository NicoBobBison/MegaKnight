using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace ChessBot.GUI
{
    internal class BoardPiece
    {
        public Vector2 BoardPosition;
        public Vector2 ScreenPosition;

        Texture2D _texture;
        const float _pieceScale = 0.06f;
        public BoardPiece(Texture2D texture)
        {
            _texture = texture;
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_texture, ScreenPosition, null, Color.White, 0, new Vector2(_texture.Width / 2, _texture.Height / 2), _pieceScale, SpriteEffects.None, 0);
        }
    }
}
