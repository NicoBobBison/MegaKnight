using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System.Reflection.Metadata;
using System.Text.Json.Serialization;
using ChessBot.Core;

namespace ChessBot.GUI
{
    internal class BoardRenderer
    {
        BotCore _core;

        #region Constants
        private Vector2 _topLeftOfBoard;
        private readonly Color _lightSquareColor = new Color(214, 198, 182);
        private readonly Color _darkSquareColor = new Color(173, 145, 116);
        private const int _tileSize = 100;
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

        List<BoardTile> _boardTiles = new List<BoardTile>();
        public BoardRenderer(ContentManager content, Vector2 screenSize)
        {
            LoadContent(content);
            _topLeftOfBoard = new Vector2(screenSize.X / 2 - 4 * _tileSize, screenSize.Y / 2 - 4 * _tileSize);
            CreateBoardTiles();
            _core = new BotCore();
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            foreach(BoardTile tile in _boardTiles)
            {
                tile.Draw(spriteBatch);
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
            Vector2 pos = _topLeftOfBoard;
            for(int r = 0; r < 8; r++)
            {
                for(int c = 0; c < 8; c++)
                {
                    Color color = (r + c) % 2 == 0 ? _lightSquareColor : _darkSquareColor;
                    BoardTile tile = new BoardTile(_tileTexture, new Vector2(pos.X, pos.Y), _tileSize, color);
                    _boardTiles.Add(tile);
                    pos.X += _tileSize;
                }
                pos.X = _topLeftOfBoard.X;
                pos.Y += _tileSize;
            }
        }
    }
}