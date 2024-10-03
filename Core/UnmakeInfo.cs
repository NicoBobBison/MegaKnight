using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MegaKnight.Core
{
    internal class UnmakeInfo
    {
        public sbyte EnPassantTargetSquare;
        public byte HalfMoveClock;
        public bool WhiteKingCastle;
        public bool WhiteQueenCastle;
        public bool BlackKingCastle;
        public bool BlackQueenCastle;
        public int BitboardIndexOfCapturedPiece = -1;
        public UnmakeInfo(sbyte enPassant, byte halfMoveClock, bool whiteKingCastle, bool whiteQueenCastle, bool blackKingCastle, bool blackQueenCastle)
        {
            EnPassantTargetSquare = enPassant;
            HalfMoveClock = halfMoveClock;
            WhiteKingCastle = whiteKingCastle;
            WhiteQueenCastle = whiteQueenCastle;
            BlackKingCastle = blackKingCastle;
            BlackQueenCastle = blackQueenCastle;
        }
    }
}
