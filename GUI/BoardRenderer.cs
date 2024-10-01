using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MegaKnight.Core;
using System.Diagnostics;
using Microsoft.Xna.Framework.Input;

namespace MegaKnight.GUI
{
    internal class BoardRenderer
    {
        const string initialFenPosition = "r4rk1/1ppp1pp1/8/8/8/1P6/2PP1PP1/R4RK1 w - - 0 1";

        public readonly BotCore Core;

        // Allow user to change what they want to promote to (defaulted to queens)
        // Press Q for queen, R for rook, B for bishop, K for knight
        // TODO: Display this information with text
        public Piece AutoPromotionPiece = Piece.Queen;
        // When the game is over, stop accepting input from player
        public bool GameOver = false;

        #region Constants
        private Vector2 _bottomRightOfBoard;
        private readonly Color _lightSquareColor = new Color(214, 198, 182);
        private readonly Color _darkSquareColor = new Color(173, 145, 116);
        private readonly Color _lightSquareHoveredColor = new Color(214, 198, 182) * 0.8f;
        private readonly Color _darkSquareHoveredColor = new Color(173, 145, 116) * 0.8f;
        public const int TileSize = 115;
        #endregion

        private Texture2D _tileTexture;
        private Texture2D _movePreviewTexture;

        #region Piece textures
        private Texture2D _whitePawn;
        private Texture2D _whiteKnight;
        private Texture2D _whiteBishop;
        private Texture2D _whiteRook;
        private Texture2D _whiteQueen;
        private Texture2D _whiteKing;

        private Texture2D _blackPawn;
        private Texture2D _blackKnight;
        private Texture2D _blackBishop;
        private Texture2D _blackRook;
        private Texture2D _blackQueen;
        private Texture2D _blackKing;
        #endregion

        public BoardTile[] BoardTiles = new BoardTile[64];
        MovePreview[] _movePreviews = new MovePreview[64];
        List<BoardPiece> _boardPieces = new List<BoardPiece>();
        public BoardRenderer(ContentManager content, Vector2 screenSize)
        {
            LoadContent(content);
            _bottomRightOfBoard = new Vector2(screenSize.X / 2 - 4 * TileSize, screenSize.Y / 2 + 3 * TileSize);
            CreateBoardTiles();
            CreateMovePreviewCircles();

            Core = new BotCore(initialFenPosition);
            RenderPosition(Core.CurrentPosition);
        }
        public void Update(GameTime gameTime)
        {
            if (InputManager.GetKeyDown(Keys.Q))
            {
                AutoPromotionPiece = Piece.Queen;
            }
            else if (InputManager.GetKeyDown(Keys.R))
            {
                AutoPromotionPiece = Piece.Rook;
            }
            else if (InputManager.GetKeyDown(Keys.B))
            {
                AutoPromotionPiece = Piece.Bishop;
            }
            else if (InputManager.GetKeyDown(Keys.K))
            {
                AutoPromotionPiece = Piece.Knight;
            }

            BoardPiece.DeletedBoardThisFrame = false;
            foreach(BoardPiece piece in _boardPieces.ToArray())
            {
                piece.Update(gameTime);
            }
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            foreach(BoardTile tile in BoardTiles)
            {
                tile.Draw(spriteBatch);
            }
            foreach (BoardPiece piece in _boardPieces)
            {
                piece.Draw(spriteBatch);
            }
            foreach (MovePreview preview in _movePreviews)
            {
                preview.Draw(spriteBatch);
            }
        }
        void LoadContent(ContentManager content)
        {
            _tileTexture = content.Load<Texture2D>("BoardTile");
            _movePreviewTexture = content.Load<Texture2D>("MovePreview");

            _whitePawn = content.Load<Texture2D>("Pieces/WhitePawn");
            _whiteKnight = content.Load<Texture2D>("Pieces/WhiteKnight");
            _whiteBishop = content.Load<Texture2D>("Pieces/WhiteBishop");
            _whiteRook = content.Load<Texture2D>("Pieces/WhiteRook");
            _whiteQueen = content.Load<Texture2D>("Pieces/WhiteQueen");
            _whiteKing = content.Load<Texture2D>("Pieces/WhiteKing");
            _blackPawn = content.Load<Texture2D>("Pieces/BlackPawn");
            _blackKnight = content.Load<Texture2D>("Pieces/BlackKnight");
            _blackBishop = content.Load<Texture2D>("Pieces/BlackBishop");
            _blackRook = content.Load<Texture2D>("Pieces/BlackRook");
            _blackQueen = content.Load<Texture2D>("Pieces/BlackQueen");
            _blackKing = content.Load<Texture2D>("Pieces/BlackKing");
        }
        void CreateBoardTiles()
        {
            Vector2 pos = _bottomRightOfBoard;
            for(int r = 0; r < 8; r++)
            {
                for(int c = 0; c < 8; c++)
                {
                    Color color = (r + c) % 2 == 1 ? _lightSquareColor : _darkSquareColor;
                    Color hoverColor = (r + c) % 2 == 1 ? _lightSquareHoveredColor : _darkSquareHoveredColor;
                    BoardTile tile = new BoardTile(_tileTexture, new Vector2(pos.X, pos.Y), TileSize, color, hoverColor, r * 8 + c);
                    BoardTiles[r * 8 + c] = tile;
                    pos.X += TileSize;
                }
                pos.X = _bottomRightOfBoard.X;
                pos.Y -= TileSize;
            }
        }
        void CreateMovePreviewCircles()
        {
            Vector2 pos = _bottomRightOfBoard + new Vector2(TileSize / 2);
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    MovePreview preview = new MovePreview(_movePreviewTexture, new Vector2(pos.X, pos.Y));
                    _movePreviews[r * 8 + c] = preview;
                    pos.X += TileSize;
                }
                pos.X = _bottomRightOfBoard.X + TileSize / 2;
                pos.Y -= TileSize;
            }
        }
        public void RenderPosition(Position position)
        {
            _boardPieces.Clear();

            RenderBitboard(position.WhitePawns, _whitePawn, Piece.Pawn, true);
            RenderBitboard(position.WhiteKnights, _whiteKnight, Piece.Knight, true);
            RenderBitboard(position.WhiteBishops, _whiteBishop, Piece.Bishop, true);
            RenderBitboard(position.WhiteRooks, _whiteRook, Piece.Rook, true);
            RenderBitboard(position.WhiteQueens, _whiteQueen, Piece.Queen, true);
            RenderBitboard(position.WhiteKing, _whiteKing, Piece.King, true);

            RenderBitboard(position.BlackPawns, _blackPawn, Piece.Pawn, false);
            RenderBitboard(position.BlackKnights, _blackKnight, Piece.Knight, false);
            RenderBitboard(position.BlackBishops, _blackBishop, Piece.Bishop, false);
            RenderBitboard(position.BlackRooks, _blackRook, Piece.Rook, false);
            RenderBitboard(position.BlackQueens, _blackQueen, Piece.Queen, false);
            RenderBitboard(position.BlackKing, _blackKing, Piece.King, false);
        }
        void RenderBitboard(ulong bitboard, Texture2D pieceTexture, Piece pieceType, bool isWhite)
        {
            List<int> boardPositions = BitboardHelper.BitboardToListOfSquareIndeces(bitboard);
            foreach(int i in boardPositions)
            {
                BoardPiece piece = new BoardPiece(pieceTexture, this, (Square)i, pieceType, isWhite);
                BoardTiles[i].Piece = piece;
                piece.ScreenPosition = BoardTiles[i].Position + new Vector2(TileSize / 2);
                _boardPieces.Add(piece);
            }
        }
        public void ClearMovePreview()
        {
            foreach(MovePreview preview in _movePreviews)
            {
                preview.IsShown = false;
            }
        }
        public void RenderMovePreview(ulong startSquare, Piece piece, Position position)
        {
            ulong possibleMoves = Core.GetLegalMoves(startSquare, piece, position);
            List<int> moveIndeces = BitboardHelper.BitboardToListOfSquareIndeces(possibleMoves);
            for(int i = 0; i < 64; i++)
            {
                _movePreviews[i].IsShown = moveIndeces.Contains(i);
            }
        }
    }
}