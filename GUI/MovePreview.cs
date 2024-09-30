using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MegaKnight.GUI
{
    internal class MovePreview
    {
        Texture2D _texture;
        Vector2 _position;
        public bool IsShown = false;
        public MovePreview(Texture2D texture, Vector2 position)
        {
            _texture = texture;
            _position = position;
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            if (IsShown)
            {
                spriteBatch.Draw(_texture, _position, null, new Color(120, 116, 113), 0, new Vector2(_texture.Width / 2, _texture.Height / 2), 1f, SpriteEffects.None, 0);
            }
        }
    }
}
