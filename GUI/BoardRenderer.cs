using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using ChessBot.Core;
using System.Diagnostics;

namespace ChessBot.GUI
{
    internal class BoardRenderer
    {
        public readonly BotCore Core;

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
            Core = new BotCore();

            Position test = new Position();
            test.WhiteToMove = true;
            test.WhiteKnights = 66ul;
            test.WhiteBishops = 36ul;
            test.WhiteRooks = 129ul;
            test.WhiteQueens = 8ul;
            RenderPosition(test);
            Core.CurrentPosition = test;
        }
        public void Update(GameTime gameTime)
        {
            foreach(BoardPiece piece in _boardPieces)
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
            foreach(BoardPiece piece in _boardPieces)
            {
                piece.Draw(spriteBatch);
            }
            foreach(MovePreview preview in _movePreviews)
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
        void RenderPosition(Position position)
        {
            RenderBitboard(position.WhitePawns, _whitePawn, Piece.Pawn);
            RenderBitboard(position.WhiteKnights, _whiteKnight, Piece.Knight);
            RenderBitboard(position.WhiteBishops, _whiteBishop, Piece.Bishop);
            RenderBitboard(position.WhiteRooks, _whiteRook, Piece.Rook);
            RenderBitboard(position.WhiteQueens, _whiteQueen, Piece.Queen);
            RenderBitboard(position.WhiteKing, _whiteKing, Piece.King);

            RenderBitboard(position.BlackPawns, _blackPawn, Piece.Pawn);
            RenderBitboard(position.BlackKnights, _blackKnight, Piece.Knight);
            RenderBitboard(position.BlackBishops, _blackBishop, Piece.Bishop);
            RenderBitboard(position.BlackRooks, _blackRook, Piece.Rook);
            RenderBitboard(position.BlackQueens, _blackQueen, Piece.Queen);
            RenderBitboard(position.BlackKing, _blackKing, Piece.King);
        }
        void RenderBitboard(ulong bitboard, Texture2D pieceTexture, Piece pieceType)
        {
            List<int> boardPositions = BoardHelper.BitboardToListOfSquareIndeces(bitboard);
            foreach(int i in boardPositions)
            {
                BoardPiece piece = new BoardPiece(pieceTexture, this, (Square)i, pieceType);
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
            List<int> moveIndeces = BoardHelper.BitboardToListOfSquareIndeces(possibleMoves);
            for(int i = 0; i < 64; i++)
            {
                _movePreviews[i].IsShown = moveIndeces.Contains(i);
            }
        }
    }
}