using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessBot.Core
{
    internal class BotCore
    {
        public Position CurrentPosition;
        MoveGenerator _moveGenerator;
        public BotCore()
        {
            _moveGenerator = new MoveGenerator();
        }
        public bool CanMakeMove(Move move, Position position)
        {
            if (move.IsWhite)
            {
                bool isBlocked = (move.EndSquare & position.WhitePieces) > 0;
                return !isBlocked;
            }
            return true;
        }
        // Precondition: Move must be legal (check with CanMakeMove())
        public Position UpdatePositionWithLegalMove(Move move, Position position)
        {
            if (move.IsWhite)
            {
                switch (move.Piece)
                {
                    case Piece.Pawn:
                        position.WhitePawns |= move.EndSquare;
                        position.WhitePawns &= ~move.StartSquare;
                        break;
                    case Piece.Knight:
                        position.WhiteKnights |= move.EndSquare;
                        position.WhiteBishops &= ~move.StartSquare;
                        break;
                    case Piece.Bishop:
                        position.WhiteBishops |= move.EndSquare;
                        position.WhiteBishops &= ~move.StartSquare;
                        break;
                    case Piece.Rook:
                        position.WhiteRooks |= move.EndSquare;
                        position.WhiteRooks &= ~move.StartSquare;
                        break;
                    case Piece.Queen:
                        position.WhiteQueens |= move.EndSquare;
                        position.WhiteQueens &= ~move.StartSquare;
                        break;
                    case Piece.King:
                        position.WhiteKing |= move.EndSquare;
                        position.WhiteKing &= ~move.StartSquare;
                        break;
                }
            }
            else
            {
                switch (move.Piece)
                {
                    case Piece.Pawn:
                        position.BlackPawns |= move.EndSquare;
                        position.BlackPawns &= ~move.StartSquare;
                        break;
                    case Piece.Knight:
                        position.BlackKnights |= move.EndSquare;
                        position.BlackKnights &= ~move.StartSquare;
                        break;
                    case Piece.Bishop:
                        position.BlackBishops |= move.EndSquare;
                        position.BlackBishops &= ~move.StartSquare;
                        break;
                    case Piece.Rook:
                        position.BlackRooks |= move.EndSquare;
                        position.BlackRooks &= ~move.StartSquare;
                        break;
                    case Piece.Queen:
                        position.BlackQueens |= move.EndSquare;
                        position.BlackQueens &= ~move.StartSquare;
                        break;
                    case Piece.King:
                        position.BlackKing |= move.EndSquare;
                        position.BlackKing &= ~move.StartSquare;
                        break;
                }
            }
            return position;
        }
    }
}
