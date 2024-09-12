using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using ChessBot.Core;

namespace ChessBot.GUI
{
    internal class BoardPiece : IEquatable<BoardPiece>
    {
        public readonly Piece Piece;
        public Square BoardPosition;
        public Vector2 ScreenPosition;

        static BoardPiece _selectedPiece = null;

        Texture2D _texture;
        BoardRenderer _renderer;
        const float _pieceScale = 0.06f;
        public BoardPiece(Texture2D texture, BoardRenderer renderer, Square boardPosition, Piece piece)
        {
            _texture = texture;
            _renderer = renderer;
            BoardPosition = boardPosition;
            Piece = piece;
        }
        public void Update(GameTime gameTime)
        {
            // On click
            if (InputManager.GetMouseDown())
            {
                // If holding this piece
                if(_selectedPiece == this)
                {
                    // Get hovered tile
                    BoardTile hoveredTile = GetHoveredBoardTile(_renderer.BoardTiles);
                    // If hovering a position on the board
                    if(hoveredTile != null)
                    {
                        // Generate move based on hovered tile
                        Square desiredSquare = (Square)hoveredTile.Index;
                        Move move = new Move(true, Piece, BoardPosition, desiredSquare); // For now, player always plays white
                        // If move is valid
                        if (_renderer.Core.CanMakeMove(move, _renderer.Core.CurrentPosition))
                        {
                            ScreenPosition = hoveredTile.Position + new Vector2(BoardRenderer.TileSize / 2);
                            BoardPosition = desiredSquare;
                            _renderer.Core.CurrentPosition = _renderer.Core.UpdatePositionWithLegalMove(move, _renderer.Core.CurrentPosition);
                        }
                    }
                    // TODO: Since this is run on all pieces, the last pieces to be updated trigger the else-if condition below 
                    _selectedPiece = null;
                    BoardTile.HoveredTile = null;
                }
                else if(_selectedPiece == null && InputManager.IsHovering(GetCollisionBox()))
                {
                    _selectedPiece = this;
                    BoardTile hoveredTile = GetHoveredBoardTile(_renderer.BoardTiles);
                    if(hoveredTile != null)
                    {
                        BoardTile.HoveredTile = hoveredTile;
                    }
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
