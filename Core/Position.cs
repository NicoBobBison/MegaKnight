using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MegaKnight.Core
{
    internal class Position : IEquatable<Position>
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

        ulong[] Bitboards => new ulong[] { WhitePawns, WhiteKnights, WhiteBishops, WhiteRooks, WhiteQueens, WhiteKing,
                                           BlackPawns, BlackKnights, BlackBishops, BlackRooks, BlackQueens, BlackKing };


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
            if (BitboardHelper.GetBitboardPopCount(piecePosition) != 1)
                throw new Exception("Can't check sliding piece if piece position ulong doesn't have exactly one piece");
            return ((WhiteBishops | WhiteRooks | WhiteQueens | BlackBishops | BlackRooks | BlackQueens) & piecePosition) > 0;
        }
        public void MakeMove(Move move)
        {
            UnmakeInfo unmakeInfo = new UnmakeInfo(EnPassantTargetSquare, HalfMoveClock, WhiteKingCastle, WhiteQueenCastle, BlackKingCastle, BlackQueenCastle);
            _unmakeInfos.Push(unmakeInfo);

            byte blackToMove = WhiteToMove ? (byte)0 : (byte)1;

            // Remove from start square
            int bitboardIndex = 6 * blackToMove + (int)move.Piece;
            SetBitboard(bitboardIndex, Bitboards[bitboardIndex] ^ move.StartSquare);

            // Remove any piece from end square
            for(int i = 6 * (1 - blackToMove); i < 6 * (1 - blackToMove) + 6; i++)
            {
                if ((Bitboards[i] & move.EndSquare) > 0)
                {
                    unmakeInfo.BitboardIndexOfCapturedPiece = i;
                    SetBitboard(i, Bitboards[i] & ~move.EndSquare);
                    break;
                }
            }

            SetBitboard(bitboardIndex, Bitboards[bitboardIndex] | move.EndSquare);

            if(move.Piece == Piece.Pawn)
            {
                if(move.MoveType == MoveType.EnPassant)
                {
                    WhitePawns &= ~(1ul << (EnPassantTargetSquare + 8));
                    BlackPawns &= ~(1ul << (EnPassantTargetSquare - 8));
                }
                else if(move.MoveType == MoveType.DoublePawnPush)
                {
                    EnPassantTargetSquare = WhiteToMove ? (sbyte)(BitboardHelper.SinglePopBitboardToIndex(move.StartSquare) + 8) : (sbyte)(BitboardHelper.SinglePopBitboardToIndex(move.StartSquare) - 8);
                }
                else if(move.MoveType == MoveType.KnightPromotion || move.MoveType == MoveType.KnightPromoCapture)
                {
                    WhitePawns &= ~move.EndSquare;
                    BlackPawns &= ~move.EndSquare;
                    int promoIndex = 6 * blackToMove + 1;
                    SetBitboard(promoIndex, Bitboards[promoIndex] | move.EndSquare);
                }
                else if (move.MoveType == MoveType.BishopPromotion || move.MoveType == MoveType.BishopPromoCapture)
                {
                    WhitePawns &= ~move.EndSquare;
                    BlackPawns &= ~move.EndSquare;
                    int promoIndex = 6 * blackToMove + 2;
                    SetBitboard(promoIndex, Bitboards[promoIndex] | move.EndSquare);
                }
                else if (move.MoveType == MoveType.RookPromotion || move.MoveType == MoveType.RookPromoCapture)
                {
                    WhitePawns &= ~move.EndSquare;
                    BlackPawns &= ~move.EndSquare;
                    int promoIndex = 6 * blackToMove + 3;
                    SetBitboard(promoIndex, Bitboards[promoIndex] | move.EndSquare);
                }
                else if (move.MoveType == MoveType.QueenPromotion || move.MoveType == MoveType.QueenPromoCapture)
                {
                    WhitePawns &= ~move.EndSquare;
                    BlackPawns &= ~move.EndSquare;
                    int promoIndex = 6 * blackToMove + 4;
                    SetBitboard(promoIndex, Bitboards[promoIndex] | move.EndSquare);
                }
            }
            else if (move.Piece == Piece.King)
            {
                if(move.MoveType == MoveType.KingCastle)
                {
                    SetBitboard(6 * blackToMove + 3, Bitboards[6 * blackToMove + 3] & ~(1ul << 7 + (blackToMove * 56)));
                    SetBitboard(6 * blackToMove + 3, Bitboards[6 * blackToMove + 3] | (1ul << 5 + (blackToMove * 56)));
                }
                else if(move.MoveType == MoveType.QueenCastle)
                {
                    SetBitboard(6 * blackToMove + 3, Bitboards[6 * blackToMove + 3] & ~(1ul << blackToMove * 56));
                    SetBitboard(6 * blackToMove + 3, Bitboards[6 * blackToMove + 3] | (1ul << 3 + (blackToMove * 56)));
                }
                if (WhiteToMove)
                {
                    WhiteKingCastle = false;
                    WhiteQueenCastle = false;
                }
                else
                {
                    BlackKingCastle = false;
                    BlackQueenCastle = false;
                }
            }
            else if(move.Piece == Piece.Rook)
            {
                if (WhiteToMove)
                {
                    if((move.StartSquare & 1ul) > 0)
                    {
                        WhiteQueenCastle = false;
                    }
                    else if ((move.StartSquare & 1ul << 7) > 0)
                    {
                        WhiteKingCastle = false;
                    }
                }
                else
                {
                    if ((move.StartSquare & 1ul << 56) > 0)
                    {
                        BlackQueenCastle = false;
                    }
                    else if ((move.StartSquare & 1ul << 63) > 0)
                    {
                        BlackKingCastle = false;
                    }
                }
            }
            if(move.MoveType != MoveType.DoublePawnPush) EnPassantTargetSquare = -1;

            HalfMoveClock++;
            WhiteToMove = !WhiteToMove;
        }
        public void UnmakeMove(Move move)
        {
            UnmakeInfo info = _unmakeInfos.Pop();
            WhiteToMove = !WhiteToMove;
            byte blackToMove = WhiteToMove ? (byte)0 : (byte)1;

            // Remove from start square
            int bitboardIndex = 6 * blackToMove + (int)move.Piece;
            SetBitboard(bitboardIndex, Bitboards[bitboardIndex] & ~move.EndSquare);
            if(info.BitboardIndexOfCapturedPiece != -1)
            {
                SetBitboard(info.BitboardIndexOfCapturedPiece, Bitboards[info.BitboardIndexOfCapturedPiece] | move.EndSquare);
            }
            if(move.Piece == Piece.Pawn)
            {
                if (move.MoveType == MoveType.EnPassant)
                {
                    if (WhiteToMove)
                    {
                        BlackPawns |= move.EndSquare >> 8;
                    }
                    else
                    {
                        WhitePawns |= move.EndSquare << 8;
                    }
                }
                else if (move.MoveType == MoveType.KnightPromotion || move.MoveType == MoveType.KnightPromoCapture)
                {
                    int promoIndex = 6 * blackToMove + 1;
                    SetBitboard(promoIndex, Bitboards[promoIndex] ^ move.EndSquare);
                }
                else if (move.MoveType == MoveType.BishopPromotion || move.MoveType == MoveType.BishopPromoCapture)
                {
                    int promoIndex = 6 * blackToMove + 2;
                    SetBitboard(promoIndex, Bitboards[promoIndex] ^ move.EndSquare);
                }
                else if (move.MoveType == MoveType.RookPromotion || move.MoveType == MoveType.RookPromoCapture)
                {
                    int promoIndex = 6 * blackToMove + 3;
                    SetBitboard(promoIndex, Bitboards[promoIndex] ^ move.EndSquare);
                }
                else if (move.MoveType == MoveType.QueenPromotion || move.MoveType == MoveType.QueenPromoCapture)
                {
                    int promoIndex = 6 * blackToMove + 4;
                    SetBitboard(promoIndex, Bitboards[promoIndex] ^ move.EndSquare);
                }

            }
            else if(move.Piece == Piece.King)
            {
                if (move.MoveType == MoveType.KingCastle)
                {
                    SetBitboard(6 * blackToMove + 3, Bitboards[6 * blackToMove + 3] & ~(1ul << 5 + (blackToMove * 56)));
                    SetBitboard(6 * blackToMove + 3, Bitboards[6 * blackToMove + 3] | (1ul << 7 + (blackToMove * 56)));
                }
                else if (move.MoveType == MoveType.QueenCastle)
                {
                    SetBitboard(6 * blackToMove + 3, Bitboards[6 * blackToMove + 3] & ~(1ul << 3 + blackToMove * 56));
                    SetBitboard(6 * blackToMove + 3, Bitboards[6 * blackToMove + 3] | (1ul << (blackToMove * 56)));
                }

            }
            SetBitboard(bitboardIndex, Bitboards[bitboardIndex] | move.StartSquare);

            WhiteKingCastle = info.WhiteKingCastle;
            WhiteQueenCastle = info.WhiteQueenCastle;
            BlackKingCastle = info.BlackKingCastle;
            BlackQueenCastle = info.BlackQueenCastle;
            EnPassantTargetSquare = info.EnPassantTargetSquare;
            HalfMoveClock = info.HalfMoveClock;
        }
        void UpdatePositionWithCaptures(Move move)
        {
            ulong endSquare = move.EndSquare;
            if (WhiteToMove)
            {
                if (move.Piece == Piece.Pawn && (move.EndSquare & (1ul << EnPassantTargetSquare)) > 0)
                {
                    // En passant capture
                    BlackPawns &= ~(1ul << EnPassantTargetSquare - 8);
                }
                else if ((BlackPieces & endSquare) > 0)
                {
                    if ((BlackPawns & endSquare) > 0)
                    {
                        BlackPawns &= ~endSquare;
                    }
                    else if ((BlackKnights & endSquare) > 0)
                    {
                        BlackKnights &= ~endSquare;
                    }
                    else if ((BlackBishops & endSquare) > 0)
                    {
                        BlackBishops &= ~endSquare;
                    }
                    else if ((BlackRooks & endSquare) > 0)
                    {
                        BlackRooks &= ~endSquare;
                    }
                    else if ((BlackQueens & endSquare) > 0)
                    {
                        BlackQueens &= ~endSquare;
                    }
                    else
                    {
                        BlackKing = 0ul;
                    }
                }
            }
            else
            {
                if (move.Piece == Piece.Pawn && (move.EndSquare & (1ul << EnPassantTargetSquare)) > 0)
                {
                    // En passant capture
                    WhitePawns &= ~(1ul << EnPassantTargetSquare + 8);
                }
                else if ((WhitePieces & endSquare) > 0)
                {
                    if ((WhitePawns & endSquare) > 0)
                    {
                        WhitePawns &= ~endSquare;
                    }
                    else if ((WhiteKnights & endSquare) > 0)
                    {
                        WhiteKnights &= ~endSquare;
                    }
                    else if ((WhiteBishops & endSquare) > 0)
                    {
                        WhiteBishops &= ~endSquare;
                    }
                    else if ((WhiteRooks & endSquare) > 0)
                    {
                        WhiteRooks &= ~endSquare;
                    }
                    else if ((WhiteQueens & endSquare) > 0)
                    {
                        WhiteQueens &= ~endSquare;
                    }
                    else
                    {
                        WhiteKing = 0ul;
                    }
                }
            }
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
        public void PrintPosition()
        {
            Debug.WriteLine("Printing position...");
            BitboardHelper.PrintBitboard(AllPieces);
        }
    }
}
