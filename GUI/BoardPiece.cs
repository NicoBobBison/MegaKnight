using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ChessBot.GUI
{
    internal class BoardPiece : IEquatable<BoardPiece>
    {
        public Vector2 BoardPosition;
        public Vector2 ScreenPosition;

        static BoardPiece _selectedPiece = null;

        Texture2D _texture;
        BoardRenderer _renderer;
        const float _pieceScale = 0.06f;
        public BoardPiece(Texture2D texture, BoardRenderer renderer)
        {
            _texture = texture;
            _renderer = renderer;
        }
        public void Update(GameTime gameTime)
        {
            if (InputManager.GetMouseDown())
            {
                if(_selectedPiece == this)
                {
                    BoardTile hoveredTile = GetHoveredBoardTile(_renderer.BoardTiles);
                    if(hoveredTile != null)
                    {
                        ulong desiredTarget = 1ul << hoveredTile.Index;

                        ScreenPosition = hoveredTile.Position + new Vector2(BoardRenderer.TileSize / 2);
                    }
                    _selectedPiece = null;
                }
                else if(InputManager.IsHovering(GetCollisionBox()))
                {
                    _selectedPiece = this;
                }
            }
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_texture, ScreenPosition, null, Color.White, 0, new Vector2(_texture.Width / 2, _texture.Height / 2), _pieceScale, SpriteEffects.None, 0);
        }

        public bool Equals(BoardPiece other)
        {
            return BoardPosition == other.BoardPosition && ScreenPosition == other.ScreenPosition;
        }
        Rectangle GetCollisionBox()
        {
            return new Rectangle((int)ScreenPosition.X - BoardRenderer.TileSize / 2, (int)ScreenPosition.Y - BoardRenderer.TileSize / 2, BoardRenderer.TileSize, BoardRenderer.TileSize);
        }
        BoardTile GetHoveredBoardTile(BoardTile[] tiles)
        {
            Vector2 mousePos = InputManager.GetMousePosition();
            foreach(BoardTile tile in tiles)
            {
                if (tile.GetBoundingBox().Contains(mousePos))
                    return tile;
            }
            return null;
        }
    }
}
