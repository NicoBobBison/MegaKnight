using System.Collections.Generic;
using System;
using System.Linq;
using MegaKnight.Debugging;

namespace MegaKnight.Core
{
    internal class BotCore
    {
        const string _fenStartingPosition = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";

        public Position CurrentPosition;
        public bool PlayerIsPlayingWhite = true;
        MoveGenerator _moveGenerator;
        PositionEvaluator _positionEvaluator;

        public Perft Perft;
/*        public BotCore(Position initialPosition)
        {
            Position.InitializeZobristHashValues();
            _moveGenerator = new MoveGenerator();
            _positionEvaluator = new PositionEvaluator(_moveGenerator, this);
            CurrentPosition = initialPosition;
            AddPositionToPreviousPositions(initialPosition);
        } */
        public BotCore()
        {
            Position.InitializeZobristHashValues();
            _moveGenerator = new MoveGenerator();
            _positionEvaluator = new PositionEvaluator(_moveGenerator, this);
            Position p = FenToPosition(_fenStartingPosition);
            CurrentPosition = p;
            AddPositionToPreviousPositions(p);

            Perft = new Perft(_moveGenerator, this);
            Perft.RunPerft(p, 5);
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
        public void MakeMove(Move move, Position position)
        {
            // TODO: Rewrite this to remove most of the branching if possible
            position.HalfMoveClock++;
            position = UpdatePositionWithCaptures(move, position);
            position.EnPassantTargetSquare = -1;
            if (move.Piece == Piece.Pawn || move.MoveType == MoveType.Capture) position.HalfMoveClock = 0;
            if (position.WhiteToMove)
            {
                switch (move.Piece)
                {
                    case Piece.Pawn:
                        position.WhitePawns ^= move.StartSquare;
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
                            position.EnPassantTargetSquare = (sbyte)(BitboardHelper.SinglePopBitboardToIndex(move.StartSquare) + 8);
                        }
                        break;
                    case Piece.Knight:
                        position.WhiteKnights |= move.EndSquare;
                        position.WhiteKnights ^= move.StartSquare;
                        break;
                    case Piece.Bishop:
                        position.WhiteBishops |= move.EndSquare;
                        position.WhiteBishops ^= move.StartSquare;
                        break;
                    case Piece.Rook:
                        if ((move.StartSquare & 1ul) > 0)
                        {
                            position.WhiteQueenCastle = false;
                        }
                        else if((move.StartSquare & 1ul << 7) > 0)
                        {
                            position.WhiteKingCastle = false;
                        }
                        position.WhiteRooks |= move.EndSquare;
                        position.WhiteRooks ^= move.StartSquare;
                        break;
                    case Piece.Queen:
                        position.WhiteQueens |= move.EndSquare;
                        position.WhiteQueens ^= move.StartSquare;
                        break;
                    case Piece.King:
                        position.WhiteKing |= move.EndSquare;
                        position.WhiteKing ^= move.StartSquare;
                        position.WhiteKingCastle = false;
                        position.WhiteQueenCastle = false;
                        if(move.MoveType == MoveType.KingCastle)
                        {
                            position.WhiteRooks |= 1ul << 5;
                            position.WhiteRooks ^= 1ul << 7;
                        }
                        else if(move.MoveType == MoveType.QueenCastle)
                        {
                            position.WhiteRooks |= 1ul << 3;
                            position.WhiteRooks ^= 1ul;
                        }
                        break;
                }
            }
            else
            {
                switch (move.Piece)
                {
                    case Piece.Pawn:
                        position.BlackPawns ^= move.StartSquare;
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
                            position.EnPassantTargetSquare = (sbyte)(BitboardHelper.SinglePopBitboardToIndex(move.StartSquare) - 8);
                        }
                        break;
                    case Piece.Knight:
                        position.BlackKnights |= move.EndSquare;
                        position.BlackKnights ^= move.StartSquare;
                        break;
                    case Piece.Bishop:
                        position.BlackBishops |= move.EndSquare;
                        position.BlackBishops ^= move.StartSquare;
                        break;
                    case Piece.Rook:
                        if ((move.StartSquare & 1ul << 56) > 0)
                        {
                            position.BlackQueenCastle = false;
                        }
                        else if ((move.StartSquare & 1ul << 63) > 0)
                        {
                            position.BlackKingCastle = false;
                        }
                        position.BlackRooks |= move.EndSquare;
                        position.BlackRooks ^= move.StartSquare;
                        break;
                    case Piece.Queen:
                        position.BlackQueens |= move.EndSquare;
                        position.BlackQueens ^= move.StartSquare;
                        break;
                    case Piece.King:
                        position.BlackKing |= move.EndSquare;
                        position.BlackKing ^= move.StartSquare;
                        position.BlackKingCastle = false;
                        position.BlackQueenCastle = false;
                        if (move.MoveType == MoveType.KingCastle)
                        {
                            position.BlackRooks |= 1ul << 61;
                            position.BlackRooks ^= 1ul << 63;
                        }
                        else if (move.MoveType == MoveType.QueenCastle)
                        {
                            position.BlackRooks |= 1ul << 59;
                            position.BlackRooks ^= 1ul << 56;
                        }
                        break;
                }
            }
            position.WhiteToMove = !position.WhiteToMove;
        }
        public void UnmakeMove(Move move, Position position)
        {
            // TODO: Implement this, since it's probably faster to unmake a move than to copy the entire board
        }
        Position UpdatePositionWithCaptures(Move move, Position position)
        {
            ulong endSquare = move.EndSquare;
            if (position.WhiteToMove)
            {
                if(move.Piece == Piece.Pawn && (move.EndSquare & (1ul << position.EnPassantTargetSquare)) > 0)
                {
                    // En passant capture
                    position.BlackPawns &= ~(1ul << position.EnPassantTargetSquare - 8);
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
                if (move.Piece == Piece.Pawn && (move.EndSquare & (1ul << position.EnPassantTargetSquare)) > 0)
                {
                    // En passant capture
                    position.WhitePawns &= ~(1ul << position.EnPassantTargetSquare + 8);
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
        public void AddPositionToPreviousPositions(Position position)
        {
            _positionEvaluator.AddPositionToPreviousPositions(position);
        }
        /// <summary>
        /// Reads in a FEN string and creates a position based on it
        /// </summary>
        /// <param name="fenString">The FEN string</param>
        /// <returns>The FEN string as a position object</returns>
        Position FenToPosition(string fenString)
        {
            Position position = new Position();
            string[] splitFen = fenString.Split(' ');
            if(splitFen.Length != 6) throw new Exception("Invalid spaces FEN string: " + fenString);

            // 0: piece positions
            string[] rowsOfPieces = splitFen[0].Split("/");
            if (rowsOfPieces.Length != 8) throw new Exception("Invalid /'s FEN string: " + fenString);
            int boardPositionCount = 56;
            foreach(string row in rowsOfPieces)
            {
                foreach (char c in row)
                {
                    if (char.IsNumber(c))
                    {
                        int charAsInt = c - '0';
                        boardPositionCount += charAsInt;
                    }
                    else
                    {
                        switch (c)
                        {
                            case 'P':
                                position.WhitePawns |= 1ul << boardPositionCount;
                                break;
                            case 'N':
                                position.WhiteKnights |= 1ul << boardPositionCount;
                                break;
                            case 'B':
                                position.WhiteBishops |= 1ul << boardPositionCount;
                                break;
                            case 'R':
                                position.WhiteRooks |= 1ul << boardPositionCount;
                                break;
                            case 'Q':
                                position.WhiteQueens |= 1ul << boardPositionCount;
                                break;
                            case 'K':
                                position.WhiteKing |= 1ul << boardPositionCount;
                                break;
                            case 'p':
                                position.BlackPawns |= 1ul << boardPositionCount;
                                break;
                            case 'n':
                                position.BlackKnights |= 1ul << boardPositionCount;
                                break;
                            case 'b':
                                position.BlackBishops |= 1ul << boardPositionCount;
                                break;
                            case 'r':
                                position.BlackRooks |= 1ul << boardPositionCount;
                                break;
                            case 'q':
                                position.BlackQueens |= 1ul << boardPositionCount;
                                break;
                            case 'k':
                                position.BlackKing |= 1ul << boardPositionCount;
                                break;
                            default:
                                throw new Exception("Invalid letter in FEN string: " + fenString);
                        }
                        boardPositionCount++;
                    }
                }
                boardPositionCount -= 16;
            }

            // 1: side to move
            if (splitFen[1].ToCharArray()[0] == 'w')
            {
                position.WhiteToMove = true;
            }
            else
            {
                position.WhiteToMove = false;
            }

            // 2: castling rights
            char[] castleRights = splitFen[2].ToCharArray();
            if (castleRights[0] == '-')
            {
                position.WhiteKingCastle = false;
                position.WhiteQueenCastle = false;
                position.BlackKingCastle = false;
                position.BlackQueenCastle = false;
            }
            else
            {
                if (castleRights.Contains('K'))
                    position.WhiteKingCastle = true;
                if (castleRights.Contains('Q'))
                    position.WhiteQueenCastle = true;
                if (castleRights.Contains('k'))
                    position.BlackKingCastle = true;
                if (castleRights.Contains('q'))
                    position.BlackQueenCastle = true;
            }

            // 3: en passant target square
            char[] enPassant = splitFen[3].ToCharArray();
            if (enPassant[0] == '-')
            {
                position.EnPassantTargetSquare = -1;
            }
            else
            {
                int col = enPassant[0] - 'a';
                int row = enPassant[1] - '1';
                position.EnPassantTargetSquare = (sbyte)(8 * row + col);
            }

            // 4: Half-move counter
            if (int.TryParse(splitFen[4], out int halfMoves))
            {
                position.HalfMoveClock = (byte)halfMoves;
            }
            else
            {
                throw new Exception("Invalid half move count for FEN string: " + fenString);
            }

            // 5: Full-move counter (is this even necessary?)
            // Don't do anything with this for now

            return position;
        }
        #region Check current position for checkmate/draw
        public bool CurrentPositionIsCheckmate()
        {
            return _positionEvaluator.IsCheckmate(CurrentPosition);
        }
        public bool CurrentPositionIsStalemate()
        {
            return _positionEvaluator.IsStalemate(CurrentPosition);
        }
        public bool CurrentPositionIsDrawByFiftyMoveRule()
        {
            return _positionEvaluator.IsDrawByFiftyMoveRule(CurrentPosition);
        }
        public bool CurrentPositionIsDrawByRepetition()
        {
            return _positionEvaluator.IsDrawByRepetition(CurrentPosition);
        }
        public bool CurrentPositionIsDrawByInsufficientMaterial()
        {
            return _positionEvaluator.IsDrawByInsufficientMaterial(CurrentPosition);
        }
        #endregion
    }
}
