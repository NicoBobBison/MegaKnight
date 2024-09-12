using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace ChessBot.GUI
{
    internal class BoardTile
    {
        // The index of the board. a1 = 0, h8 = 63
        public readonly int Index;

        Texture2D _texture;
        public Vector2 Position;
        int _size;
        Color _color;
        public BoardTile(Texture2D texture, Vector2 position, int size, Color color, int index)
        {
            _texture = texture;
            Position = position;
            _size = size;
            _color = color;
            Index = index;
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_texture, new Rectangle((int)Position.X, (int)Position.Y, _size, _size), _color);
        }
        public Rectangle GetBoundingBox()
        {
            return new Rectangle((int)Position.X, (int)Position.Y, _size, _size);
        }
    }
}
