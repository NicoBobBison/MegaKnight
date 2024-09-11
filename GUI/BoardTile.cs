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
        Texture2D _texture;
        Vector2 _position;
        int _size;
        Color _color;
        public BoardTile(Texture2D texture, Vector2 position, int size, Color color)
        {
            _texture = texture;
            _position = position;
            _size = size;
            _color = color;
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_texture, new Rectangle((int)_position.X, (int)_position.Y, _size, _size), _color);
        }
    }
}
