using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessBot.Core
{
    public enum Piece
    {
        Pawn,
        Knight,
        Bishop,
        Rook,
        Queen,
        King
    }
    public enum Square
    {
        a1, b1, c1, d1, e1, f1, g1, h1,
        a2, b2, c2, d2, e2, f2, g2, h2,
        a3, b3, c3, d3, e3, f3, g3, h3,
        a4, b4, c4, d4, e4, f4, g4, h4,
        a5, b5, c5, d5, e5, f5, g5, h5,
        a6, b6, c6, d6, e6, f6, g6, h6,
        a7, b7, c7, d7, e7, f7, g7, h7,
        a8, b8, c8, d8, e8, f8, g8, h8
    };

    internal class Move
    {
        public readonly bool IsWhite;
        public readonly Piece Piece;
        public readonly ulong StartSquare;
        public readonly ulong EndSquare;
        public Move(bool isWhite, Piece piece, Square startSquare, Square endSquare)
        {
            IsWhite = isWhite;
            Piece = piece;
            StartSquare = SquareToBitboard(startSquare);
            EndSquare = SquareToBitboard(endSquare);
        }
        ulong SquareToBitboard(Square square)
        {
            return 1ul << (int)square;
        }
    }
}
