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
        BotCore _core;

        #region Constants
        private Vector2 _bottomRightOfBoard;
        private readonly Color _lightSquareColor = new Color(214, 198, 182);
        private readonly Color _darkSquareColor = new Color(173, 145, 116);
        private const int _tileSize = 115;
        #endregion

        private Texture2D _tileTexture;

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

        BoardTile[] _boardTiles = new BoardTile[64];
        List<BoardPiece> _boardPieces = new List<BoardPiece>();
        public BoardRenderer(ContentManager content, Vector2 screenSize)
        {
            LoadContent(content);
            _bottomRightOfBoard = new Vector2(screenSize.X / 2 - 4 * _tileSize, screenSize.Y / 2 + 3 * _tileSize);
            CreateBoardTiles();
            _core = new BotCore();

            Position test = new Position();
            test.WhiteRooks = 128ul;
            RenderPosition(test);
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            foreach(BoardTile tile in _boardTiles)
            {
                tile.Draw(spriteBatch);
            }
            foreach(BoardPiece piece in _boardPieces)
            {
                piece.Draw(spriteBatch);
            }
        }
        void LoadContent(ContentManager content)
        {
            _tileTexture = content.Load<Texture2D>("BoardTile");

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
                    BoardTile tile = new BoardTile(_tileTexture, new Vector2(pos.X, pos.Y), _tileSize, color);
                    _boardTiles[r * 8 + c] = tile;
                    pos.X += _tileSize;
                }
                pos.X = _bottomRightOfBoard.X;
                pos.Y -= _tileSize;
            }
        }
        void RenderPosition(Position position)
        {
            RenderBitboard(position.WhitePawns, _whitePawn);
            RenderBitboard(position.WhiteKnights, _whiteKnight);
            RenderBitboard(position.WhiteBishops, _whiteBishop);
            RenderBitboard(position.WhiteRooks, _whiteRook);
            RenderBitboard(position.WhiteQueens, _whiteQueen);
            RenderBitboard(position.WhiteKing, _whiteKing);

            RenderBitboard(position.BlackPawns, _blackPawn);
            RenderBitboard(position.BlackKnights, _blackKnight);
            RenderBitboard(position.BlackBishops, _blackBishop);
            RenderBitboard(position.BlackRooks, _blackRook);
            RenderBitboard(position.BlackQueens, _blackQueen);
            RenderBitboard(position.BlackKing, _blackKing);
        }
        void RenderBitboard(ulong bitboard, Texture2D pieceTexture)
        {
            List<int> boardPositions = BoardHelper.BitboardToListOfSquareIndeces(bitboard);
            foreach(int i in boardPositions)
            {
                BoardPiece piece = new BoardPiece(pieceTexture);
                piece.ScreenPosition = _boardTiles[i].Position + new Vector2(_tileSize / 2);
                _boardPieces.Add(piece);
            }
        }
    }
}