using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MegaKnight.Core
{
    internal class Position : IEquatable<Position>, ICloneable
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

        public ulong[] Bitboards => new ulong[] { WhitePawns, WhiteKnights, WhiteBishops, WhiteRooks, WhiteQueens, WhiteKing,
                                                  BlackPawns, BlackKnights, BlackBishops, BlackRooks, BlackQueens, BlackKing };

        public ulong HashValue { get; private set; }

        // Other information
        public bool WhiteToMove;
        public sbyte EnPassantTargetSquare = -1;
        public byte HalfMoveClock = 0;

        public bool WhiteKingCastle;
        public bool WhiteQueenCastle;
        public bool BlackKingCastle;
        public bool BlackQueenCastle;

        // Other helpful bitboards
        public ulong WhitePieces => WhitePawns | WhiteKnights | WhiteBishops | WhiteRooks | WhiteQueens | WhiteKing;
        public ulong BlackPieces => BlackPawns | BlackKnights | BlackBishops | BlackRooks | BlackQueens | BlackKing;
        public ulong AllPieces => WhitePieces | BlackPieces;
        static ulong[] _zobristHashValues;
        static Stack<UnmakeInfo> _unmakeInfos = new Stack<UnmakeInfo>();
        public bool IsSlidingPiece(ulong piecePosition)
        {
            if (Helper.GetBitboardPopCount(piecePosition) != 1)
                throw new Exception("Can't check sliding piece if piece position ulong doesn't have exactly one piece");
            return ((WhiteBishops | WhiteRooks | WhiteQueens | BlackBishops | BlackRooks | BlackQueens) & piecePosition) > 0;
        }
        public void MakeMove(Move move)
        {
            UnmakeInfo unmakeInfo = new UnmakeInfo(EnPassantTargetSquare, HalfMoveClock, WhiteKingCastle, WhiteQueenCastle, BlackKingCastle, BlackQueenCastle);

            byte blackToMove = WhiteToMove ? (byte)0 : (byte)1;
            HalfMoveClock++;

            // Remove from start square
            int bitboardIndex = 6 * blackToMove + (int)move.Piece;
            SetBitboard(bitboardIndex, Bitboards[bitboardIndex] ^ move.StartSquare);
            HashValue ^= GetZobristValueForPieceSquare(bitboardIndex, move.StartSquare);

            // Remove any piece from end square
            for(int i = 6 * (1 - blackToMove); i < 6 * (1 - blackToMove) + 6; i++)
            {
                if ((Bitboards[i] & move.EndSquare) > 0)
                {
                    unmakeInfo.BitboardIndexOfCapturedPiece = i;
                    SetBitboard(i, Bitboards[i] & ~move.EndSquare);
                    HashValue ^= GetZobristValueForPieceSquare(i, move.EndSquare);
                    break;
                }
            }

            SetBitboard(bitboardIndex, Bitboards[bitboardIndex] | move.EndSquare);
            HashValue ^= GetZobristValueForPieceSquare(bitboardIndex, move.EndSquare);

            if (move.MoveType == MoveType.Capture) HalfMoveClock = 0;
            if (move.Piece == Piece.Pawn)
            {
                HalfMoveClock = 0;
                if(move.MoveType == MoveType.EnPassant)
                {
                    if (WhiteToMove)
                    {
                        BlackPawns &= ~(1ul << (EnPassantTargetSquare - 8));
                        HashValue ^= GetZobristValueForPieceSquare(6, 1ul << (EnPassantTargetSquare - 8));
                    }
                    else
                    {
                        WhitePawns &= ~(1ul << (EnPassantTargetSquare + 8));
                        HashValue ^= GetZobristValueForPieceSquare(0, 1ul << (EnPassantTargetSquare + 8));
                    }
                }
                else if(move.MoveType == MoveType.DoublePawnPush)
                {
                    if (EnPassantTargetSquare != -1)
                    {
                        HashValue ^= _zobristHashValues[64 * 12 + 1 + 4 + EnPassantTargetSquare % 8];
                    }
                    if (WhiteToMove)
                    {
                        EnPassantTargetSquare = (sbyte)(Helper.SinglePopBitboardToIndex(move.StartSquare) + 8);
                    }
                    else
                    {
                        EnPassantTargetSquare = (sbyte)(Helper.SinglePopBitboardToIndex(move.StartSquare) - 8);
                    }
                    HashValue ^= _zobristHashValues[64 * 12 + 1 + 4 + EnPassantTargetSquare % 8];
                }
                else if(move.MoveType == MoveType.KnightPromotion || move.MoveType == MoveType.KnightPromoCapture)
                {
                    if (WhiteToMove)
                    {
                        WhitePawns &= ~move.EndSquare;
                        HashValue ^= GetZobristValueForPieceSquare(0, move.EndSquare);
                    }
                    else
                    {
                        BlackPawns &= ~move.EndSquare;
                        HashValue ^= GetZobristValueForPieceSquare(6, move.EndSquare);
                    }
                    int promoIndex = 6 * blackToMove + 1;
                    SetBitboard(promoIndex, Bitboards[promoIndex] ^ move.EndSquare);
                    HashValue ^= GetZobristValueForPieceSquare(promoIndex, move.EndSquare);
                }
                else if (move.MoveType == MoveType.BishopPromotion || move.MoveType == MoveType.BishopPromoCapture)
                {
                    if (WhiteToMove)
                    {
                        WhitePawns &= ~move.EndSquare;
                        HashValue ^= GetZobristValueForPieceSquare(0, move.EndSquare);
                    }
                    else
                    {
                        BlackPawns &= ~move.EndSquare;
                        HashValue ^= GetZobristValueForPieceSquare(6, move.EndSquare);
                    }
                    int promoIndex = 6 * blackToMove + 2;
                    SetBitboard(promoIndex, Bitboards[promoIndex] ^ move.EndSquare);
                    HashValue ^= GetZobristValueForPieceSquare(promoIndex, move.EndSquare);
                }
                else if (move.MoveType == MoveType.RookPromotion || move.MoveType == MoveType.RookPromoCapture)
                {
                    if (WhiteToMove)
                    {
                        WhitePawns &= ~move.EndSquare;
                        HashValue ^= GetZobristValueForPieceSquare(0, move.EndSquare);
                    }
                    else
                    {
                        BlackPawns &= ~move.EndSquare;
                        HashValue ^= GetZobristValueForPieceSquare(6, move.EndSquare);
                    }
                    int promoIndex = 6 * blackToMove + 3;
                    SetBitboard(promoIndex, Bitboards[promoIndex] ^ move.EndSquare);
                    HashValue ^= GetZobristValueForPieceSquare(promoIndex, move.EndSquare);
                }
                else if (move.MoveType == MoveType.QueenPromotion || move.MoveType == MoveType.QueenPromoCapture)
                {
                    if (WhiteToMove)
                    {
                        WhitePawns &= ~move.EndSquare;
                        HashValue ^= GetZobristValueForPieceSquare(0, move.EndSquare);
                    }
                    else
                    {
                        BlackPawns &= ~move.EndSquare;
                        HashValue ^= GetZobristValueForPieceSquare(6, move.EndSquare);
                    }
                    int promoIndex = 6 * blackToMove + 4;
                    SetBitboard(promoIndex, Bitboards[promoIndex] ^ move.EndSquare);
                    HashValue ^= GetZobristValueForPieceSquare(promoIndex, move.EndSquare);
                }
            }
            else if (move.Piece == Piece.King)
            {
                if(move.MoveType == MoveType.KingCastle)
                {
                    SetBitboard(6 * blackToMove + 3, Bitboards[6 * blackToMove + 3] & ~(1ul << 7 + (blackToMove * 56)));
                    HashValue ^= GetZobristValueForPieceSquare(6 * blackToMove + 3, 1ul << 7 + (blackToMove * 56));
                    SetBitboard(6 * blackToMove + 3, Bitboards[6 * blackToMove + 3] | (1ul << 5 + (blackToMove * 56)));
                    HashValue ^= GetZobristValueForPieceSquare(6 * blackToMove + 3, 1ul << 5 + (blackToMove * 56));
                }
                else if(move.MoveType == MoveType.QueenCastle)
                {
                    SetBitboard(6 * blackToMove + 3, Bitboards[6 * blackToMove + 3] & ~(1ul << blackToMove * 56));
                    HashValue ^= GetZobristValueForPieceSquare(6 * blackToMove + 3, 1ul << (blackToMove * 56));
                    SetBitboard(6 * blackToMove + 3, Bitboards[6 * blackToMove + 3] | (1ul << 3 + (blackToMove * 56)));
                    HashValue ^= GetZobristValueForPieceSquare(6 * blackToMove + 3, 1ul << 3 + (blackToMove * 56));
                }
                if (WhiteToMove)
                {
                    if(WhiteKingCastle) HashValue ^= _zobristHashValues[64 * 12 + 1];
                    WhiteKingCastle = false;
                    if(WhiteQueenCastle) HashValue ^= _zobristHashValues[64 * 12 + 2];
                    WhiteQueenCastle = false;
                }
                else
                {
                    if(BlackKingCastle) HashValue ^= _zobristHashValues[64 * 12 + 3];
                    BlackKingCastle = false;
                    if(BlackQueenCastle) HashValue ^= _zobristHashValues[64 * 12 + 4];
                    BlackQueenCastle = false;
                }
            }
            else if(move.Piece == Piece.Rook)
            {
                if (WhiteToMove)
                {
                    if((move.StartSquare & 1ul) > 0)
                    {
                        if(WhiteQueenCastle) HashValue ^= _zobristHashValues[64 * 12 + 2];
                        WhiteQueenCastle = false;
                    }
                    else if ((move.StartSquare & 1ul << 7) > 0)
                    {
                        if(WhiteKingCastle) HashValue ^= _zobristHashValues[64 * 12 + 1];
                        WhiteKingCastle = false;
                    }
                }
                else
                {
                    if ((move.StartSquare & 1ul << 56) > 0)
                    {
                        if(BlackQueenCastle) HashValue ^= _zobristHashValues[64 * 12 + 4];
                        BlackQueenCastle = false;
                    }
                    else if ((move.StartSquare & 1ul << 63) > 0)
                    {
                        if(BlackKingCastle) HashValue ^= _zobristHashValues[64 * 12 + 3];
                        BlackKingCastle = false;
                    }
                }
            }
            if (move.MoveType != MoveType.DoublePawnPush)
            {
                if (EnPassantTargetSquare != -1)
                {
                    HashValue ^= _zobristHashValues[64 * 12 + 1 + 4 + EnPassantTargetSquare % 8];
                }
                EnPassantTargetSquare = -1;
            }

            WhiteToMove = !WhiteToMove;
            HashValue ^= _zobristHashValues[64 * 12];

            _unmakeInfos.Push(unmakeInfo);
        }
        public void UnmakeMove(Move move)
        {
            UnmakeInfo info = _unmakeInfos.Pop();
            WhiteToMove = !WhiteToMove;
            HashValue ^= _zobristHashValues[64 * 12];

            byte blackToMove = WhiteToMove ? (byte)0 : (byte)1;

            // Remove from end square
            int bitboardIndex = 6 * blackToMove + (int)move.Piece;
            if (!move.IsPromotion())
            {
                SetBitboard(bitboardIndex, Bitboards[bitboardIndex] & ~move.EndSquare);
                HashValue ^= GetZobristValueForPieceSquare(bitboardIndex, move.EndSquare);
            }

            if (info.BitboardIndexOfCapturedPiece != -1)
            {
                SetBitboard(info.BitboardIndexOfCapturedPiece, Bitboards[info.BitboardIndexOfCapturedPiece] | move.EndSquare);
                HashValue ^= GetZobristValueForPieceSquare(info.BitboardIndexOfCapturedPiece, move.EndSquare);
            }
            if (move.Piece == Piece.Pawn)
            {
                if (move.MoveType == MoveType.EnPassant)
                {
                    if (WhiteToMove)
                    {
                        BlackPawns |= move.EndSquare >> 8;
                        HashValue ^= GetZobristValueForPieceSquare(6, move.EndSquare >> 8);
                    }
                    else
                    {
                        WhitePawns |= move.EndSquare << 8;
                        HashValue ^= GetZobristValueForPieceSquare(0, move.EndSquare << 8);
                    }
                }
                else if (move.MoveType == MoveType.KnightPromotion || move.MoveType == MoveType.KnightPromoCapture)
                {
                    int promoIndex = 6 * blackToMove + 1;
                    SetBitboard(promoIndex, Bitboards[promoIndex] & ~move.EndSquare);
                    HashValue ^= GetZobristValueForPieceSquare(promoIndex, move.EndSquare);
                }
                else if (move.MoveType == MoveType.BishopPromotion || move.MoveType == MoveType.BishopPromoCapture)
                {
                    int promoIndex = 6 * blackToMove + 2;
                    SetBitboard(promoIndex, Bitboards[promoIndex] & ~move.EndSquare);
                    HashValue ^= GetZobristValueForPieceSquare(promoIndex, move.EndSquare);
                }
                else if (move.MoveType == MoveType.RookPromotion || move.MoveType == MoveType.RookPromoCapture)
                {
                    int promoIndex = 6 * blackToMove + 3;
                    SetBitboard(promoIndex, Bitboards[promoIndex] & ~move.EndSquare);
                    HashValue ^= GetZobristValueForPieceSquare(promoIndex, move.EndSquare);
                }
                else if (move.MoveType == MoveType.QueenPromotion || move.MoveType == MoveType.QueenPromoCapture)
                {
                    int promoIndex = 6 * blackToMove + 4;
                    SetBitboard(promoIndex, Bitboards[promoIndex] & ~move.EndSquare);
                    HashValue ^= GetZobristValueForPieceSquare(promoIndex, move.EndSquare);
                }
            }
            else if(move.Piece == Piece.King)
            {
                if (move.MoveType == MoveType.KingCastle)
                {
                    SetBitboard(6 * blackToMove + 3, Bitboards[6 * blackToMove + 3] & ~(1ul << 5 + (blackToMove * 56)));
                    HashValue ^= GetZobristValueForPieceSquare(6 * blackToMove + 3, 1ul << 5 + (blackToMove * 56));
                    SetBitboard(6 * blackToMove + 3, Bitboards[6 * blackToMove + 3] | (1ul << 7 + (blackToMove * 56)));
                    HashValue ^= GetZobristValueForPieceSquare(6 * blackToMove + 3, 1ul << 7 + (blackToMove * 56));
                }
                else if (move.MoveType == MoveType.QueenCastle)
                {
                    SetBitboard(6 * blackToMove + 3, Bitboards[6 * blackToMove + 3] & ~(1ul << 3 + blackToMove * 56));
                    HashValue ^= GetZobristValueForPieceSquare(6 * blackToMove + 3, 1ul << 3 + (blackToMove * 56));
                    SetBitboard(6 * blackToMove + 3, Bitboards[6 * blackToMove + 3] | (1ul << (blackToMove * 56)));
                    HashValue ^= GetZobristValueForPieceSquare(6 * blackToMove + 3, 1ul << (blackToMove * 56));
                }
            }
            SetBitboard(bitboardIndex, Bitboards[bitboardIndex] | move.StartSquare);
            HashValue ^= GetZobristValueForPieceSquare(bitboardIndex, move.StartSquare);

            if(WhiteKingCastle != info.WhiteKingCastle)
            {
                WhiteKingCastle = info.WhiteKingCastle;
                HashValue ^= _zobristHashValues[64 * 12 + 1];
            }
            if (WhiteQueenCastle != info.WhiteQueenCastle)
            {
                WhiteQueenCastle = info.WhiteQueenCastle;
                HashValue ^= _zobristHashValues[64 * 12 + 2];
            }
            if (BlackKingCastle != info.BlackKingCastle)
            {
                BlackKingCastle = info.BlackKingCastle;
                HashValue ^= _zobristHashValues[64 * 12 + 3];
            }
            if (BlackQueenCastle != info.BlackQueenCastle)
            {
                BlackQueenCastle = info.BlackQueenCastle;
                HashValue ^= _zobristHashValues[64 * 12 + 4];
            }
            if(EnPassantTargetSquare != info.EnPassantTargetSquare)
            {
                if (EnPassantTargetSquare != -1)
                {
                    HashValue ^= _zobristHashValues[64 * 12 + 1 + 4 + EnPassantTargetSquare % 8];
                }
                EnPassantTargetSquare = info.EnPassantTargetSquare;
                if (info.EnPassantTargetSquare != -1)
                {
                    HashValue ^= _zobristHashValues[64 * 12 + 1 + 4 + info.EnPassantTargetSquare % 8];
                }
            }
            HalfMoveClock = info.HalfMoveClock;
        }
        public void MakeNullMove()
        {
            UnmakeInfo unmakeInfo = new UnmakeInfo(EnPassantTargetSquare, HalfMoveClock, WhiteKingCastle, WhiteQueenCastle, BlackKingCastle, BlackQueenCastle);
            WhiteToMove = !WhiteToMove;
            HashValue ^= _zobristHashValues[64 * 12];
            _unmakeInfos.Push(unmakeInfo);
            HalfMoveClock++;
        }
        public void UnmakeNullMove()
        {
            UnmakeInfo info = _unmakeInfos.Pop();
            WhiteToMove = !WhiteToMove;
            HashValue ^= _zobristHashValues[64 * 12];

            if (WhiteKingCastle != info.WhiteKingCastle)
            {
                WhiteKingCastle = info.WhiteKingCastle;
                HashValue ^= _zobristHashValues[64 * 12 + 1];
            }
            if (WhiteQueenCastle != info.WhiteQueenCastle)
            {
                WhiteQueenCastle = info.WhiteQueenCastle;
                HashValue ^= _zobristHashValues[64 * 12 + 2];
            }
            if (BlackKingCastle != info.BlackKingCastle)
            {
                BlackKingCastle = info.BlackKingCastle;
                HashValue ^= _zobristHashValues[64 * 12 + 3];
            }
            if (BlackQueenCastle != info.BlackQueenCastle)
            {
                BlackQueenCastle = info.BlackQueenCastle;
                HashValue ^= _zobristHashValues[64 * 12 + 4];
            }
            if (EnPassantTargetSquare != info.EnPassantTargetSquare)
            {
                if (EnPassantTargetSquare != -1) HashValue ^= _zobristHashValues[64 * 12 + 1 + 4 + EnPassantTargetSquare % 8];
                EnPassantTargetSquare = info.EnPassantTargetSquare;
                if (info.EnPassantTargetSquare != -1) HashValue ^= _zobristHashValues[64 * 12 + 1 + 4 + info.EnPassantTargetSquare % 8];
            }
            HalfMoveClock = info.HalfMoveClock;
        }
        void SetBitboard(int index, ulong val)
        {
            switch (index)
            {
                case 0:
                    WhitePawns = val;
                    break;
                case 1:
                    WhiteKnights = val;
                    break;
                case 2:
                    WhiteBishops = val;
                    break;
                case 3:
                    WhiteRooks = val;
                    break;
                case 4:
                    WhiteQueens = val;
                    break;
                case 5:
                    WhiteKing = val;
                    break;
                case 6:
                    BlackPawns = val;
                    break;
                case 7:
                    BlackKnights = val;
                    break;
                case 8:
                    BlackBishops = val;
                    break;
                case 9:
                    BlackRooks = val;
                    break;
                case 10:
                    BlackQueens = val;
                    break;
                case 11:
                    BlackKing = val;
                    break;
            }
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
                EnPassantTargetSquare == other.EnPassantTargetSquare &&
                HalfMoveClock == other.HalfMoveClock;
        }

        // Zobrist hashing
        // https://www.chessprogramming.org/Zobrist_Hashing
        public void InitializeHash()
        {
            List<ulong> toHash = new List<ulong>();
            ulong[] piecesAsList = new ulong[] { WhitePawns, WhiteKnights, WhiteBishops, WhiteRooks, WhiteQueens, WhiteKing,
                                                 BlackPawns, BlackKnights, BlackBishops, BlackRooks, BlackQueens, BlackKing };
            for(int pieceCount = 0; pieceCount < piecesAsList.Length; pieceCount++)
            {
                foreach (int i in Helper.BoardToArrayOfIndeces(piecesAsList[pieceCount]))
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

            if (toHash.Count == 0) HashValue = 0;
            ulong hash = toHash[0];
            for(int i = 1; i < toHash.Count; i++)
            {
                hash ^= toHash[i];
            }
            HashValue = hash;
        }
        ulong GetZobristValueForPieceSquare(int pieceIndex, ulong squareIndex)
        {
            return _zobristHashValues[64 * pieceIndex + Helper.SinglePopBitboardToIndex(squareIndex)];
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
        public override string ToString()
        {
            string str = "";
            ulong i = 1ul;
            for (int r = 0; r < 8; r++)
            {
                string rowStr = "";
                for (int c = 0; c < 8; c++)
                {
                    if ((WhitePawns & i) > 0) rowStr += "P ";
                    else if ((WhiteKnights & i) > 0) rowStr += "N ";
                    else if ((WhiteBishops & i) > 0) rowStr += "B ";
                    else if ((WhiteRooks & i) > 0) rowStr += "R ";
                    else if ((WhiteQueens & i) > 0) rowStr += "Q ";
                    else if ((WhiteKing & i) > 0) rowStr += "K ";
                    else if ((BlackPawns & i) > 0) rowStr += "p ";
                    else if ((BlackKnights & i) > 0) rowStr += "n ";
                    else if ((BlackBishops & i) > 0) rowStr += "b ";
                    else if ((BlackRooks & i) > 0) rowStr += "r ";
                    else if ((BlackQueens & i) > 0) rowStr += "q ";
                    else if ((BlackKing & i) > 0) rowStr += "k ";
                    else rowStr += "- ";
                    i <<= 1;
                }
                str = "\n" + rowStr + str;
            }
            str += "\nEn passant square: " + EnPassantTargetSquare;
            str += "\nHalf move clock: " + HalfMoveClock;
            return str + "\n";

        }
        public string AllLegalMovesToString(MoveGenerator moveGenerator)
        {
            string str = "";
            foreach(Move move in moveGenerator.GenerateAllPossibleMoves(this))
            {
                str += move.ToString();
                str += " ";
            }
            return str;
        }
        /// <summary>
        /// Should always use make/unmake instead of clone, this is just for debugging purposes
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
