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
        public static BoardTile HoveredTile = null;

        Texture2D _texture;
        public Vector2 Position;
        int _size;
        Color _normalColor;
        Color _selectedColor;
        public BoardTile(Texture2D texture, Vector2 position, int size, Color normalColor, Color selectedColor, int index)
        {
            _texture = texture;
            Position = position;
            _size = size;
            _normalColor = normalColor;
            _selectedColor = selectedColor;
            Index = index;
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            Color c = HoveredTile != null && HoveredTile.Index == Index ? _selectedColor : _normalColor;
            spriteBatch.Draw(_texture, new Rectangle((int)Position.X, (int)Position.Y, _size, _size), c);
        }
        public Rectangle GetBoundingBox()
        {
            return new Rectangle((int)Position.X, (int)Position.Y, _size, _size);
        }
    }
}
