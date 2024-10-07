using System.Collections.Generic;
using System;
using System.Linq;
using MegaKnight.Debugging;
using System.Diagnostics;

namespace MegaKnight.Core
{
    internal class BotCore
    {
        const string _fenStartingPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        public Position CurrentPosition;
        public bool PlayerIsPlayingWhite = true;
        public bool PlayingAgainstEngine = true;
        MoveGenerator _moveGenerator;
        Evaluator _evaluator;
        Engine _engine;

        public Perft Perft;
        public BotCore()
        {
            Position.InitializeZobristHashValues();

            _moveGenerator = new MoveGenerator();
            _evaluator = new Evaluator(_moveGenerator, this);
            _engine = new Engine(_moveGenerator, _evaluator);

            Position p = FenToPosition(_fenStartingPosition);
            CurrentPosition = p;
            AddPositionToPreviousPositions(p);

            // Perft = new Perft(_moveGenerator, this);
            // Perft.RunPerft(p, 6);
            if(!PlayerIsPlayingWhite) MakeEngineMove();
        }

        public bool CanMakeMove(Move move, Position position)
        {
            ulong possibleMoves = _moveGenerator.GenerateMoves(move.StartSquare, move.Piece, position);
            return (possibleMoves & move.EndSquare) > 0;
        }
        public void MakeMoveOnCurrentPosition(Move move)
        {
            CurrentPosition.MakeMove(move);
            if (CurrentPositionIsGameOver()) return;
        }
        public void MakeEngineMove()
        {
            Move engineMove = _engine.GetBestMove(CurrentPosition);
            CurrentPosition.MakeMove(engineMove);
        }
        public ulong GetLegalMoves(ulong startSquare, Piece piece, Position position)
        {
            return _moveGenerator.GenerateMoves(startSquare, piece, position);
        }
        // Precondition: Move must be legal (check with CanMakeMove())
        public void AddPositionToPreviousPositions(Position position)
        {
            _evaluator.AddPositionToPreviousPositions(position);
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
        public bool CurrentPositionIsGameOver()
        {
            return CurrentPositionIsCheckmate() || CurrentPositionIsStalemate() ||
                   CurrentPositionIsDrawByFiftyMoveRule() || CurrentPositionIsDrawByRepetition() || 
                   CurrentPositionIsDrawByInsufficientMaterial();
        }
        public bool CurrentPositionIsCheckmate()
        {
            return _evaluator.IsCheckmate(CurrentPosition);
        }
        public bool CurrentPositionIsStalemate()
        {
            return _evaluator.IsStalemate(CurrentPosition);
        }
        public bool CurrentPositionIsDrawByFiftyMoveRule()
        {
            return _evaluator.IsDrawByFiftyMoveRule(CurrentPosition);
        }
        public bool CurrentPositionIsDrawByRepetition()
        {
            return _evaluator.IsDrawByRepetition(CurrentPosition);
        }
        public bool CurrentPositionIsDrawByInsufficientMaterial()
        {
            return _evaluator.IsDrawByInsufficientMaterial(CurrentPosition);
        }
        #endregion
    }
}
