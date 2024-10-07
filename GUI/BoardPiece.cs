using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MegaKnight.Core;
using System.Diagnostics;

namespace MegaKnight.GUI
{
    internal class BoardPiece : IEquatable<BoardPiece>
    {
        readonly bool _isWhite;
        public readonly Piece Piece;
        public Square BoardPosition;
        public Vector2 ScreenPosition;
        public static bool DeletedBoardThisFrame = false;

        static BoardPiece _selectedPiece = null;
        Vector2 _tempScreenPosition;

        Texture2D _texture;
        BoardRenderer _renderer;
        const float _pieceScale = 0.06f;
        public BoardPiece(Texture2D texture, BoardRenderer renderer, Square boardPosition, Piece piece, bool isWhite)
        {
            _texture = texture;
            _renderer = renderer;
            BoardPosition = boardPosition;
            Piece = piece;
            _isWhite = isWhite;
        }
        public void Update(GameTime gameTime)
        {
            if (_renderer.GameOver) return;
            if (DeletedBoardThisFrame) return;

            bool playerInteractionCondition = _renderer.Core.CurrentPosition.WhiteToMove == _isWhite;
            // bool playerInteractionCondition = _renderer.Core.PlayerIsPlayingWhite == _isWhite;

            BoardTile hoveredTile = GetHoveredBoardTile(_renderer.BoardTiles);

            // On click, start dragging a piece (if it's the color to move). Mark the piece as selected.
            if (InputManager.GetMouseDown() && playerInteractionCondition)
            {
                if(_selectedPiece == this)
                {
                    if (hoveredTile != _renderer.BoardTiles[(int)BoardPosition])
                    {
                        TryMakeMove(hoveredTile);
                    }
                    else
                    {
                        ScreenPosition = _tempScreenPosition;
                    }
                }
                else if (GetCollisionBox().Contains(InputManager.GetMousePosition()))
                {
                    _selectedPiece = this;
                    ulong square = 1ul << (int)BoardPosition;
                    _renderer.RenderMovePreview(square, Piece, _renderer.Core.CurrentPosition);
                    _tempScreenPosition = ScreenPosition;
                }
            }
            else if (InputManager.GetMousePressed() && _selectedPiece == this)
            {
                ScreenPosition = InputManager.GetMousePosition();
            }
            // On mouse up, test if the marked piece can move to that square.
            if (InputManager.GetMouseUp() && _selectedPiece == this)
            {
                if(hoveredTile != _renderer.BoardTiles[(int)BoardPosition])
                {
                    TryMakeMove(hoveredTile);
                }
                else
                {
                    ScreenPosition = _tempScreenPosition;
                }
            }
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_texture, ScreenPosition, null, Color.White, 0, new Vector2(_texture.Width / 2, _texture.Height / 2), _pieceScale, SpriteEffects.None, 0);
        }
        void TryMakeMove(BoardTile hoveredTile)
        {
            if (hoveredTile == null)
            {
                ScreenPosition = _tempScreenPosition;
                _selectedPiece = null;
                _renderer.ClearMovePreview();
                return;
            }
            // Generate move based on hovered tile
            Square desiredSquare = (Square)hoveredTile.Index;
            if (!_renderer.Core.PlayerIsPlayingWhite)
            {
                desiredSquare = (Square)((int)desiredSquare ^ 56 ^ 7);
            }
            Move move = new Move(Piece, BoardPosition, desiredSquare);
            int squareIndex = Helper.SinglePopBitboardToIndex(move.EndSquare);
            
            if (Piece == Piece.Pawn)
            {
                if((_isWhite && squareIndex > 55) || (!_isWhite && squareIndex < 8))
                {
                    // Don't need to flag promotion capture for player's UI moves (I think) because flags are only used for analysis
                    move.FlagPromotion(_renderer.AutoPromotionPiece);
                }
                else if (Math.Abs((int)BoardPosition - (int)desiredSquare) == 16)
                {
                    move.MoveType = MoveType.DoublePawnPush;
                }
                else if ((int)desiredSquare == _renderer.Core.CurrentPosition.EnPassantTargetSquare)
                {
                    move.MoveType = MoveType.EnPassant;
                }
            }
            if (Piece == Piece.King)
            {
                if (BoardPosition - desiredSquare == 2)
                {
                    move.MoveType = MoveType.QueenCastle;
                }
                else if (BoardPosition - desiredSquare == -2)
                {
                    move.MoveType = MoveType.KingCastle;
                }
            }
            if (_renderer.Core.CanMakeMove(move, _renderer.Core.CurrentPosition))
            {
                // If we can move, redraw the board based on the current position
                _renderer.ClearMovePreview();
                _renderer.Core.MakeMoveOnCurrentPosition(move);
                _renderer.RenderPosition(_renderer.Core.CurrentPosition);
                if(_renderer.Core.PlayingAgainstEngine) _renderer.Core.MakeEngineMove();
                _renderer.RenderPosition(_renderer.Core.CurrentPosition);

                DeletedBoardThisFrame = true;
                if (!CheckIfGameOver() && Piece != Piece.Pawn)
                {
                    _renderer.Core.AddPositionToPreviousPositions(_renderer.Core.CurrentPosition);
                }
            }
            else
            {
                ScreenPosition = _tempScreenPosition;
                _selectedPiece = null;
                _renderer.ClearMovePreview();
            }
        }

        private bool CheckIfGameOver()
        {
            if (_renderer.Core.CurrentPositionIsCheckmate())
            {
                Debug.WriteLine(string.Format("Checkmate! {0} wins.", _renderer.Core.CurrentPosition.WhiteToMove ? "Black" : "White"));
                _renderer.GameOver = true;
                return true;
            }
            else if (_renderer.Core.CurrentPositionIsStalemate())
            {
                Debug.WriteLine("Stalemate. Draw.");
                _renderer.GameOver = true;
                return true;
            }
            else if (_renderer.Core.CurrentPositionIsDrawByFiftyMoveRule())
            {
                Debug.WriteLine("Draw by 50 move rule.");
                _renderer.GameOver = true;
                return true;
            }
            else if (_renderer.Core.CurrentPositionIsDrawByRepetition())
            {
                Debug.WriteLine("Draw by repetition.");
                _renderer.GameOver = true;
                return true;
            }
            else if (_renderer.Core.CurrentPositionIsDrawByInsufficientMaterial())
            {
                Debug.WriteLine("Draw by insufficient material.");
                _renderer.GameOver = true;
                return true;
            }
            return false;
        }

        public bool Equals(BoardPiece other)
        {
            return BoardPosition == other.BoardPosition && ScreenPosition == other.ScreenPosition && Piece == other.Piece;
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
