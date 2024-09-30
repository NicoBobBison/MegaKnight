using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MegaKnight.Core
{
    internal class Position : ICloneable, IEquatable<Position>
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
        public int EnPassantTargetSquare = -1;
        public int HalfMoveClock = 0;

        public bool WhiteKingCastle = true;
        public bool WhiteQueenCastle = true;
        public bool BlackKingCastle = true;
        public bool BlackQueenCastle = true;

        // Other helpful bitboards
        public ulong WhitePieces => WhitePawns | WhiteKnights | WhiteBishops | WhiteRooks | WhiteQueens | WhiteKing;
        public ulong BlackPieces => BlackPawns | BlackKnights | BlackBishops | BlackRooks | BlackQueens | BlackKing;
        public ulong AllPieces => WhitePieces | BlackPieces;
        static ulong[] _zobristHashValues;
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
        public bool Equals(Position other)
        {
            return WhitePawns == other.WhitePawns &&
                WhiteKnights == other.WhiteKnights &&
                WhiteBishops == other.WhiteBishops &&
                WhiteRooks == other.WhiteRooks &&
                WhiteQueens == other.WhiteQueens &&
                WhiteKing == other.WhiteKing &&
                BlackPawns == other.BlackPawns &&
                BlackKnights == other.BlackKnights &&
                BlackBishops == other.BlackBishops &&
                BlackRooks == other.BlackRooks &&
                BlackQueens == other.BlackQueens &&
                BlackKing == other.BlackKing &&
                WhiteToMove == other.WhiteToMove &&
                WhiteKingCastle == other.WhiteKingCastle &&
                WhiteQueenCastle == other.WhiteQueenCastle &&
                BlackKingCastle == other.BlackKingCastle &&
                BlackQueenCastle == other.BlackQueenCastle &&
                EnPassantTargetSquare == other.EnPassantTargetSquare;
        }

        // Zobrist hashing
        // https://www.chessprogramming.org/Zobrist_Hashing
        public ulong Hash()
        {
            List<ulong> toHash = new List<ulong>();
            ulong[] piecesAsList = new ulong[] { WhitePawns, WhiteKnights, WhiteBishops, WhiteRooks, WhiteQueens, WhiteKing,
                                                 BlackPawns, BlackKnights, BlackBishops, BlackRooks, BlackQueens, BlackKing };
            for(int pieceCount = 0; pieceCount < piecesAsList.Length; pieceCount++)
            {
                foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(piecesAsList[pieceCount]))
                {
                    toHash.Add(_zobristHashValues[64 * pieceCount + i]);
                }
            }
            // At this point, we've used indeces 0 to 12*64 - 1 random numbers
            if (!WhiteToMove)     toHash.Add(_zobristHashValues[64 * 12]);
            if (WhiteKingCastle)  toHash.Add(_zobristHashValues[64 * 12 + 1]);
            if (WhiteQueenCastle) toHash.Add(_zobristHashValues[64 * 12 + 2]);
            if (BlackKingCastle)  toHash.Add(_zobristHashValues[64 * 12 + 3]);
            if (BlackQueenCastle) toHash.Add(_zobristHashValues[64 * 12 + 4]);

            if (EnPassantTargetSquare != -1) toHash.Add(_zobristHashValues[64 * 12 + 1 + 4 + EnPassantTargetSquare % 8]);

            if (toHash.Count == 0) return 0ul;
            ulong hash = toHash[0];
            for(int i = 1; i < toHash.Count; i++)
            {
                hash ^= toHash[i];
            }
            return hash;
        }
        public static void InitializeZobristHashValues()
        {
            _zobristHashValues = new ulong[781];
            Random r = new Random();
            for(int i = 0; i < 781; i++)
            {
                _zobristHashValues[i] = Convert.ToUInt64(r.NextInt64());
            }
        }
    }
}
