using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MegaKnight.Core
{
    internal class Position : ICloneable
    {
        // Bitboard representation of pieces. 64 bit long integer, with each bit representing a square.
        // a1 = least significant bit (right most), h8 = most significant bit (left most)
        public ulong WhitePawns;
        public ulong WhiteKnights;
        public ulong WhiteBishops;
        public ulong WhiteRooks;
        public ulong WhiteQueens;
        public ulong WhiteKing;

        public ulong BlackPawns;
        public ulong BlackKnights;
        public ulong BlackBishops;
        public ulong BlackRooks;
        public ulong BlackQueens;
        public ulong BlackKing;

        // Other information
        public bool WhiteToMove;
        public int WhiteEnPassantIndex = -1;
        public int BlackEnPassantIndex = -1;
        public int HalfMoveClock = 0;

        public bool WhiteKingCastle = true;
        public bool WhiteQueenCastle = true;
        public bool BlackKingCastle = true;
        public bool BlackQueenCastle = true;

        // Other helpful bitboards
        public ulong WhitePieces => WhitePawns | WhiteKnights | WhiteBishops | WhiteRooks | WhiteQueens | WhiteKing;
        public ulong BlackPieces => BlackPawns | BlackKnights | BlackBishops | BlackRooks | BlackQueens | BlackKing;
        public ulong AllPieces => WhitePieces | BlackPieces;
        public bool IsSlidingPiece(ulong piecePosition)
        {
            if (BitboardHelper.GetBitboardPopCount(piecePosition) != 1)
                throw new Exception("Can't check sliding piece if piece position ulong doesn't have exactly one piece");
            return ((WhiteBishops | WhiteRooks | WhiteQueens | BlackBishops | BlackRooks | BlackQueens) & piecePosition) > 0;
        }
        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
