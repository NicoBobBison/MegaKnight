using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MegaKnight.Core
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
    }
    public enum MoveType
    {
        QuietMove,
        DoublePawnPush,
        KingCastle,
        QueenCastle,
        Capture,
        EnPassant,
        KnightPromotion,
        BishopPromotion,
        RookPromotion,
        QueenPromotion,
        KnightPromoCapture,
        BishopPromoCapture,
        RookPromoCapture,
        QueenPromoCapture
    }

    internal class Move : IEquatable<Move>
    {
        public readonly Piece Piece;
        public readonly ulong StartSquare;
        public readonly ulong EndSquare;
        public MoveType MoveType = MoveType.QuietMove; // TODO: store as ushort
        public Move(Piece piece, Square startSquare, Square endSquare, MoveType moveType = MoveType.QuietMove)
        {
            Piece = piece;
            StartSquare = SquareToBitboard(startSquare);
            EndSquare = SquareToBitboard(endSquare);
            MoveType = moveType;
        }
        ulong SquareToBitboard(Square square)
        {
            return 1ul << (int)square;
        }
        public void FlagPromotion(Piece promotionPiece)
        {
            switch (promotionPiece)
            {
                case Piece.Queen:
                    MoveType = MoveType.QueenPromotion;
                    break;
                case Piece.Rook:
                    MoveType = MoveType.RookPromotion;
                    break;
                case Piece.Bishop:
                    MoveType = MoveType.BishopPromotion;
                    break;
                case Piece.Knight:
                    MoveType = MoveType.KnightPromotion;
                    break;
                default:
                    throw new Exception("Cannot promote to piece types king or pawn");
            }
        }
        public void FlagCapturePromotion(Piece promotionPiece)
        {
            switch (promotionPiece)
            {
                case Piece.Queen:
                    MoveType = MoveType.QueenPromoCapture;
                    break;
                case Piece.Rook:
                    MoveType = MoveType.RookPromoCapture;
                    break;
                case Piece.Bishop:
                    MoveType = MoveType.BishopPromoCapture;
                    break;
                case Piece.Knight:
                    MoveType = MoveType.KnightPromoCapture;
                    break;
                default:
                    throw new Exception("Cannot promote to piece types king or pawn");
            }
        }
        public bool IsCapture()
        {
            return MoveType == MoveType.Capture || MoveType == MoveType.KnightPromoCapture || MoveType == MoveType.BishopPromoCapture ||
                   MoveType == MoveType.RookPromoCapture || MoveType == MoveType.QueenPromoCapture || MoveType == MoveType.EnPassant;
        }
        public bool IsPromotion()
        {
            return MoveType == MoveType.KnightPromotion || MoveType == MoveType.KnightPromoCapture || MoveType == MoveType.BishopPromotion || MoveType == MoveType.BishopPromoCapture ||
                   MoveType == MoveType.RookPromotion   || MoveType == MoveType.RookPromoCapture   || MoveType == MoveType.QueenPromotion  || MoveType == MoveType.QueenPromoCapture;
        }
        public Piece GetPieceCapturing(Position position)
        {
            if (!IsCapture()) throw new Exception("Not capturing a piece");
            if (MoveType == MoveType.EnPassant) return Piece.Pawn;
            if ((EndSquare & (position.WhitePawns | position.BlackPawns)) > 0) return Piece.Pawn;
            if ((EndSquare & (position.WhiteKnights | position.BlackKnights)) > 0) return Piece.Knight;
            if ((EndSquare & (position.WhiteBishops | position.BlackBishops)) > 0) return Piece.Bishop;
            if ((EndSquare & (position.WhiteRooks | position.BlackRooks)) > 0) return Piece.Rook;
            if ((EndSquare & (position.WhiteQueens | position.BlackQueens)) > 0) return Piece.Queen;
            throw new Exception("Could not get the captured piece type.");
        }
        public override string ToString()
        {
            int startSquareIndex = Helper.SinglePopBitboardToIndex(StartSquare);
            int endSquareIndex = Helper.SinglePopBitboardToIndex(EndSquare);
            string promotionStr = "";
            if(MoveType == MoveType.KnightPromotion || MoveType == MoveType.KnightPromoCapture)
            {
                promotionStr = "n";
            }
            else if (MoveType == MoveType.BishopPromotion || MoveType == MoveType.BishopPromoCapture)
            {
                promotionStr = "b";
            }
            else if (MoveType == MoveType.RookPromotion || MoveType == MoveType.RookPromoCapture)
            {
                promotionStr = "r";
            }
            else if (MoveType == MoveType.QueenPromotion || MoveType == MoveType.QueenPromoCapture)
            {
                promotionStr = "q";
            }
            return Enum.GetName((Square)startSquareIndex) + Enum.GetName((Square)endSquareIndex) + promotionStr;
        }
        public string DetailedToString(Position position)
        {
            string baseString = ToString();
            string captureString = IsCapture() ? " " + (int)Piece + "x" + (int)GetPieceCapturing(position) : "";
            return baseString + captureString;
        }

        public bool Equals(Move other)
        {
            return Piece == other.Piece && StartSquare == other.StartSquare && EndSquare == other.EndSquare && MoveType == other.MoveType;
        }
    }
}
