using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Data;

namespace MegaKnight.Core
{
    internal class Evaluator
    {
        MoveGenerator _moveGenerator;
        BotCore _core;

        const int _prevPositionsCapacity = 100000;
        Dictionary<int, List<Position>> _previousPositions;
        public Evaluator(MoveGenerator moveGenerator, BotCore core)
        {
            _moveGenerator = moveGenerator;
            _core = core;
            _previousPositions = new Dictionary<int, List<Position>>(_prevPositionsCapacity);
        }
        /// <summary>
        /// Evaluates a position based on various strategies.
        /// </summary>
        /// <param name="position">Position to evaluate</param>
        /// <returns>The evaluation result relative to the side playing (positive = good, negative = bad, 0 = drawn).</returns>
        public int Evaluate(Position position)
        {
            int whiteToMove = position.WhiteToMove ? 1 : -1;

            if (IsCheckmate(position))
            {
                return -1000000;
            }
            if (IsDraw(position))
            {
                return 0;
            }

            ulong enemyPieces = position.WhiteToMove ? position.BlackPieces : position.WhitePieces;
            ulong friendlyKing = position.WhiteToMove ? position.WhiteKing : position.BlackKing;
            ulong enemyKing = position.WhiteToMove ? position.BlackKing : position.WhiteKing;
            // Mop-up evaluation when enemy only has a king
            if((enemyPieces ^ enemyKing) == 0)
            {
                return 50 * Helper.CenterManhattanDistance(enemyKing) + 30 * (14 - Helper.ManhattanDistance(friendlyKing, enemyKing));
            }

            int evaluation = 0;

            // Value from 0 to 1 representing how far in the game we are. 0 = early game, 1 = late game.
            float gamePhase = GetGamePhase(position);

            int[] whitePawnIndeces = Helper.BoardToArrayOfIndeces(position.WhitePawns);
            int[] whiteKnightIndeces = Helper.BoardToArrayOfIndeces(position.WhiteKnights);
            int[] whiteBishopIndeces = Helper.BoardToArrayOfIndeces(position.WhiteBishops);
            int[] whiteRookIndeces = Helper.BoardToArrayOfIndeces(position.WhiteRooks);
            int[] whiteQueenIndeces = Helper.BoardToArrayOfIndeces(position.WhiteQueens);
            int[] whiteKingIndex = Helper.BoardToArrayOfIndeces(position.WhiteKing);

            int[] blackPawnIndeces = Helper.BoardToArrayOfIndeces(position.BlackPawns);
            int[] blackKnightIndeces = Helper.BoardToArrayOfIndeces(position.BlackKnights);
            int[] blackBishopIndeces = Helper.BoardToArrayOfIndeces(position.BlackBishops);
            int[] blackRookIndeces = Helper.BoardToArrayOfIndeces(position.BlackRooks);
            int[] blackQueenIndeces = Helper.BoardToArrayOfIndeces(position.BlackQueens);
            int[] blackKingIndex = Helper.BoardToArrayOfIndeces(position.BlackKing);

            int[][] allIndeces = new int[][] { whitePawnIndeces, whiteKnightIndeces, whiteBishopIndeces, whiteRookIndeces, whiteQueenIndeces, whiteKingIndex,
                                               blackPawnIndeces, blackKnightIndeces, blackBishopIndeces, blackRookIndeces, blackQueenIndeces, blackKingIndex };

            int pawnDiff = whitePawnIndeces.Length - blackPawnIndeces.Length;
            int knightDiff = whiteKnightIndeces.Length - blackKnightIndeces.Length;
            int bishopDiff = whiteBishopIndeces.Length - blackBishopIndeces.Length;
            int rookDiff = whiteRookIndeces.Length - blackRookIndeces.Length;
            int queenDiff = whiteQueenIndeces.Length - blackQueenIndeces.Length;
            
            evaluation += (int)(Helper.Lerp(EvalWeights.PawnValueEarly, EvalWeights.PawnValueLate, gamePhase) * pawnDiff);
            evaluation += (int)(Helper.Lerp(EvalWeights.KnightValueEarly, EvalWeights.KnightValueLate, gamePhase) * knightDiff);
            evaluation += (int)(Helper.Lerp(EvalWeights.BishopValueEarly, EvalWeights.BishopValueLate, gamePhase) * bishopDiff);
            evaluation += (int)(Helper.Lerp(EvalWeights.RookValueEarly, EvalWeights.RookValueLate, gamePhase) * rookDiff);
            evaluation += (int)(Helper.Lerp(EvalWeights.QueenValueEarly, EvalWeights.QueenValueLate, gamePhase) * queenDiff);

            // TODO: See if this check can be faster, or if it's even worth it to calculate (might be good with just PST)
            //evaluation += EvalWeights.PawnMobilityValue * (CalculateMobility(position.WhitePawns, Piece.Pawn, position) - CalculateMobility(position.BlackPawns, Piece.Pawn, position));
            //evaluation += EvalWeights.KnightMobilityValue * (CalculateMobility(position.WhiteKnights, Piece.Knight, position) - CalculateMobility(position.BlackKnights, Piece.Knight, position));
            //evaluation += EvalWeights.BishopMobilityValue * (CalculateMobility(position.WhiteBishops, Piece.Bishop, position) - CalculateMobility(position.BlackBishops, Piece.Bishop, position));
            //evaluation += EvalWeights.RookMobilityValue * (CalculateMobility(position.WhiteRooks, Piece.Rook, position) - CalculateMobility(position.BlackRooks, Piece.Rook, position));
            //evaluation += EvalWeights.QueenMobilityValue * (CalculateMobility(position.WhiteQueens, Piece.Queen, position) - CalculateMobility(position.BlackQueens, Piece.Queen, position));

            // PST for white pieces
            for(int i = 0; i < 6; i++)
            {
                foreach(int square in allIndeces[i])
                {
                    evaluation += (int)Helper.Lerp(EvalWeights.PSTEarly[i][square ^ 56], EvalWeights.PSTLate[i][square ^ 56], gamePhase);
                }
            }

            // PST for black pieces
            for (int i = 0; i < 6; i++)
            {
                foreach (int square in allIndeces[i + 6])
                {
                    evaluation -= (int)Helper.Lerp(EvalWeights.PSTEarly[i][square], EvalWeights.PSTLate[i][square], gamePhase);
                }
            }

            return evaluation * whiteToMove;
        }
        int CalculateMobility(ulong pieces, Piece pieceType, Position position)
        {
            int mobility = 0;
            foreach(int pieceIndex in Helper.BoardToArrayOfIndeces(pieces))
            {
                mobility += Helper.GetBitboardPopCount(_moveGenerator.GenerateMoves(1ul << pieceIndex, pieceType, position));
            }
            return mobility;
        }
        public float GetGamePhase(Position position)
        {
            float materialOnBoard = Helper.GetBitboardPopCount(position.WhitePawns | position.BlackPawns) * EvalWeights.PawnValueEarly +
                        Helper.GetBitboardPopCount(position.WhiteKnights | position.BlackKnights) * EvalWeights.KnightValueEarly +
                        Helper.GetBitboardPopCount(position.WhiteBishops | position.BlackBishops) * EvalWeights.BishopValueEarly +
                        Helper.GetBitboardPopCount(position.WhiteRooks | position.BlackRooks) * EvalWeights.RookValueEarly +
                        Helper.GetBitboardPopCount(position.WhiteQueens | position.BlackQueens) * EvalWeights.QueenValueEarly;

            float totalPossibleMaterial = 16 * EvalWeights.PawnValueEarly +
                                           4 * EvalWeights.KnightValueEarly +
                                           4 * EvalWeights.BishopValueEarly +
                                           4 * EvalWeights.RookValueEarly +
                                           2 * EvalWeights.QueenValueEarly;

            float gamePhase = (totalPossibleMaterial - materialOnBoard) / totalPossibleMaterial;
            return Math.Clamp(gamePhase, 0, 1);
        }
        public void ClearPreviousPositions()
        {
            _previousPositions.Clear();
        }
        public bool IsCheckmate(Position position)
        {
            // If we're not in check, it's not checkmate
            if (_moveGenerator.GetPiecesAttackingKing(position) == 0) return false;

            List<Move> allMoves = _moveGenerator.GenerateAllPossibleMoves(position);
            foreach (Move move in allMoves)
            {
                position.MakeMove(move);
                if (_moveGenerator.GetPiecesAttackingKing(position) == 0)
                {
                    position.UnmakeMove(move);
                    return false;
                }
                position.UnmakeMove(move);
            }
            // Every move still leaves the king in check
            return true;
        }
        public bool IsDraw(Position position)
        {
            return IsStalemate(position) || IsDrawByFiftyMoveRule(position) || IsDrawByInsufficientMaterial(position) || IsDrawByRepetition(position);
        }
        public bool IsStalemate(Position position)
        {
            return _moveGenerator.GetPiecesAttackingKing(position) == 0 && _moveGenerator.GenerateAllPossibleMoves(position).Count == 0;
        }
        public bool IsDrawByFiftyMoveRule(Position position)
        {
            return position.HalfMoveClock >= 100;
        }
        public bool IsDrawByInsufficientMaterial(Position position)
        {
            bool noQueensOrRooksOrPawns = (position.WhiteQueens | position.BlackQueens | position.WhiteRooks | position.BlackRooks | position.WhitePawns | position.BlackPawns) == 0;
            bool OneOrLessKnightOrBishopTotal = Helper.GetBitboardPopCount(position.WhiteKnights | position.WhiteBishops | position.BlackKnights | position.BlackBishops) <= 1;
            bool whiteHasTwoKnightsOnly = Helper.GetBitboardPopCount(position.WhiteKnights) == 2 && (position.BlackKnights | position.BlackBishops | position.WhiteBishops) == 0;
            bool blackHasTwoKnightsOnly = Helper.GetBitboardPopCount(position.BlackKnights) == 2 && (position.WhiteKnights | position.WhiteBishops | position.BlackBishops) == 0;
            return noQueensOrRooksOrPawns && (OneOrLessKnightOrBishopTotal || whiteHasTwoKnightsOnly || blackHasTwoKnightsOnly);
        }
        public bool IsDrawByRepetition(Position position)
        {
            int hash = (int)(position.HashValue % _prevPositionsCapacity);
            int count = 0;
            if(_previousPositions.ContainsKey(hash))
            {
                foreach(Position p in _previousPositions[hash])
                {
                    if (p.Equals(position))
                    {
                        count++;
                        if (count == 2) return true;
                    }
                }
            }
            return false;
        }
        public void AddPositionToPreviousPositions(Position position)
        {
            int hash = (int)(position.HashValue % _prevPositionsCapacity);
            if(!_previousPositions.ContainsKey(hash))
            {
                _previousPositions.Add(hash, new List<Position>());
            }
            _previousPositions[hash].Add(position);
        }
        public void RemovePositionFromPreviousPositions(Position position)
        {
            int hash = (int)(position.HashValue % _prevPositionsCapacity);
            if (_previousPositions.ContainsKey(hash))
            {
                _previousPositions[hash].Remove(position);
            }
        }
    }
}
