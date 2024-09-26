namespace ChessBot.Core
{
    internal class BotCore
    {
        public Position CurrentPosition;
        public bool PlayerIsPlayingWhite = true;
        MoveGenerator _moveGenerator;
        public BotCore()
        {
            _moveGenerator = new MoveGenerator();
        }
        public bool CanMakeMove(Move move, Position position)
        {
            ulong possibleMoves = _moveGenerator.GenerateMoves(move.StartSquare, move.Piece, position);
            return (possibleMoves & move.EndSquare) > 0;
        }
        public ulong GetLegalMoves(ulong startSquare, Piece piece, Position position)
        {
            return _moveGenerator.GenerateMoves(startSquare, piece, position);
        }
        // Precondition: Move must be legal (check with CanMakeMove())
        public Position UpdatePositionWithLegalMove(Move move, Position position)
        {
            position = UpdatePositionWithCaptures(move, position);
            position.WhiteEnPassantIndex = -1;
            position.BlackEnPassantIndex = -1;
            if (position.WhiteToMove)
            {
                switch (move.Piece)
                {
                    case Piece.Pawn:
                        position.WhitePawns &= ~move.StartSquare;
                        // Check promotion
                        if (move.MoveType == MoveType.QueenPromotion || move.MoveType == MoveType.QueenPromoCapture)
                        {
                            position.WhiteQueens |= move.EndSquare;
                        }
                        else if(move.MoveType == MoveType.RookPromotion || move.MoveType == MoveType.RookPromoCapture)
                        {
                            position.WhiteRooks |= move.EndSquare;
                        }
                        else if(move.MoveType == MoveType.BishopPromotion || move.MoveType == MoveType.BishopPromoCapture)
                        {
                            position.WhiteBishops |= move.EndSquare;
                        }
                        else if(move.MoveType == MoveType.KnightPromotion || move.MoveType == MoveType.KnightPromoCapture)
                        {
                            position.WhiteKnights |= move.EndSquare;
                        }
                        else 
                        {
                            position.WhitePawns |= move.EndSquare;
                        }
                        if (move.EndSquare == move.StartSquare << 16)
                        {
                            // Update en passant
                            position.WhiteEnPassantIndex = BitboardHelper.SinglePopBitboardToIndex(move.StartSquare) + 8;
                        }

                        break;
                    case Piece.Knight:
                        position.WhiteKnights |= move.EndSquare;
                        position.WhiteKnights &= ~move.StartSquare;
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
                        position.BlackPawns &= ~move.StartSquare;
                        // Check promotion
                        if (move.MoveType == MoveType.QueenPromotion || move.MoveType == MoveType.QueenPromoCapture)
                        {
                            position.BlackQueens |= move.EndSquare;
                        }
                        else if (move.MoveType == MoveType.RookPromotion || move.MoveType == MoveType.RookPromoCapture)
                        {
                            position.BlackRooks |= move.EndSquare;
                        }
                        else if (move.MoveType == MoveType.BishopPromotion || move.MoveType == MoveType.BishopPromoCapture)
                        {
                            position.BlackBishops |= move.EndSquare;
                        }
                        else if (move.MoveType == MoveType.KnightPromotion || move.MoveType == MoveType.KnightPromoCapture)
                        {
                            position.BlackKnights |= move.EndSquare;
                        }
                        else
                        {
                            position.BlackPawns |= move.EndSquare;
                        }
                        if (move.EndSquare == move.StartSquare >> 16)
                        {
                            // Update en passant
                            position.BlackEnPassantIndex = BitboardHelper.SinglePopBitboardToIndex(move.StartSquare) - 8;
                        }
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
            position.WhiteToMove = !position.WhiteToMove;
            return position;
        }
        Position UpdatePositionWithCaptures(Move move, Position position)
        {
            ulong endSquare = move.EndSquare;
            if (position.WhiteToMove)
            {
                if(move.Piece == Piece.Pawn && (move.EndSquare & (1ul << position.BlackEnPassantIndex)) > 0)
                {
                    // En passant capture
                    position.BlackPawns &= ~(1ul << position.BlackEnPassantIndex - 8);
                }
                else if((position.BlackPieces & endSquare) > 0)
                {
                    if((position.BlackPawns & endSquare) > 0)
                    {
                        position.BlackPawns &= ~endSquare;
                    }
                    else if((position.BlackKnights & endSquare) > 0)
                    {
                        position.BlackKnights &= ~endSquare;
                    }
                    else if((position.BlackBishops & endSquare) > 0)
                    {
                        position.BlackBishops &= ~endSquare;
                    }
                    else if((position.BlackRooks & endSquare) > 0)
                    {
                        position.BlackRooks &= ~endSquare;
                    }
                    else if((position.BlackQueens & endSquare) > 0)
                    {
                        position.BlackQueens &= ~endSquare;
                    }
                    else
                    {
                        position.BlackKing = 0ul;
                    }
                }
            }
            else
            {
                if (move.Piece == Piece.Pawn && (move.EndSquare & (1ul << position.WhiteEnPassantIndex)) > 0)
                {
                    // En passant capture
                    position.WhitePawns &= ~(1ul << position.WhiteEnPassantIndex + 8);
                }
                else if ((position.WhitePieces & endSquare) > 0)
                {
                    if ((position.WhitePawns & endSquare) > 0)
                    {
                        position.WhitePawns &= ~endSquare;
                    }
                    else if ((position.WhiteKnights & endSquare) > 0)
                    {
                        position.WhiteKnights &= ~endSquare;
                    }
                    else if ((position.WhiteBishops & endSquare) > 0)
                    {
                        position.WhiteBishops &= ~endSquare;
                    }
                    else if ((position.WhiteRooks & endSquare) > 0)
                    {
                        position.WhiteRooks &= ~endSquare;
                    }
                    else if ((position.WhiteQueens & endSquare) > 0)
                    {
                        position.WhiteQueens &= ~endSquare;
                    }
                    else
                    {
                        position.WhiteKing = 0ul;
                    }
                }
            }

            return position;
        }
    }
}
