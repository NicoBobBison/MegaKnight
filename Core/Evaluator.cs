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
        const int _prevPositionsCapacity = 500;
        Dictionary<int, List<Position>> _previousPositions;
        public Evaluator(MoveGenerator moveGenerator, BotCore core)
        {
            _moveGenerator = moveGenerator;
            _core = core;
            _previousPositions = new Dictionary<int, List<Position>>(_prevPositionsCapacity);
        }
        /// <summary>
        /// Evaluates a position based on various heuristics.
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

            int evaluation = 0;

            // All weights are in centipawns (1 centipawn = 0.01 pawns)
            // Weight for value of a piece
            const int pawnValue = 100;
            const int knightValue = 300;
            const int bishopValue = 300;
            const int rookValue = 500;
            const int queenValue = 900;

            // Weight for mobility of pieces
            const int pawnMobilityValue = 1;
            const int knightMobilityValue = 3;
            const int bishopMobilityValue = 3;
            const int rookMobilityValue = 2;
            const int queenMobilityValue = 1;

            evaluation += pawnValue * (BitboardHelper.BoardToArrayOfIndeces(position.WhitePawns).Length - BitboardHelper.BoardToArrayOfIndeces(position.BlackPawns).Length);
            evaluation += knightValue * (BitboardHelper.BoardToArrayOfIndeces(position.WhiteKnights).Length - BitboardHelper.BoardToArrayOfIndeces(position.BlackKnights).Length);
            evaluation += bishopValue * (BitboardHelper.BoardToArrayOfIndeces(position.WhiteBishops).Length - BitboardHelper.BoardToArrayOfIndeces(position.BlackBishops).Length);
            evaluation += rookValue * (BitboardHelper.BoardToArrayOfIndeces(position.WhiteRooks).Length - BitboardHelper.BoardToArrayOfIndeces(position.BlackRooks).Length);
            evaluation += queenValue * (BitboardHelper.BoardToArrayOfIndeces(position.WhiteQueens).Length - BitboardHelper.BoardToArrayOfIndeces(position.BlackQueens).Length);

            evaluation += pawnMobilityValue * (CalculateMobility(position.WhitePawns, Piece.Pawn, position) - CalculateMobility(position.BlackPawns, Piece.Pawn, position));
            evaluation += knightMobilityValue * (CalculateMobility(position.WhiteKnights, Piece.Knight, position) - CalculateMobility(position.BlackKnights, Piece.Knight, position));
            evaluation += bishopMobilityValue * (CalculateMobility(position.WhiteBishops, Piece.Bishop, position) - CalculateMobility(position.BlackBishops, Piece.Bishop, position));
            evaluation += rookMobilityValue * (CalculateMobility(position.WhiteRooks, Piece.Rook, position) - CalculateMobility(position.BlackRooks, Piece.Rook, position));
            evaluation += queenMobilityValue * (CalculateMobility(position.WhiteQueens, Piece.Queen, position) - CalculateMobility(position.BlackQueens, Piece.Queen, position));

            return evaluation * whiteToMove;
        }
        int CalculateMobility(ulong pieces, Piece pieceType, Position position)
        {
            int mobility = 0;
            foreach(int pieceIndex in BitboardHelper.BoardToArrayOfIndeces(pieces))
            {
                mobility += BitboardHelper.GetBitboardPopCount(_moveGenerator.GenerateMoves(1ul << pieceIndex, pieceType, position));
            }
            return mobility;
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
            bool OneOrLessKnightOrBishopTotal = BitboardHelper.GetBitboardPopCount(position.WhiteKnights | position.WhiteBishops | position.BlackKnights | position.BlackBishops) <= 1;
            bool whiteHasTwoKnightsOnly = BitboardHelper.GetBitboardPopCount(position.WhiteKnights) == 2 && (position.BlackKnights | position.BlackBishops | position.WhiteBishops) == 0;
            bool blackHasTwoKnightsOnly = BitboardHelper.GetBitboardPopCount(position.BlackKnights) == 2 && (position.WhiteKnights | position.WhiteBishops | position.BlackBishops) == 0;
            return noQueensOrRooksOrPawns && (OneOrLessKnightOrBishopTotal || whiteHasTwoKnightsOnly || blackHasTwoKnightsOnly);
        }
        public bool IsDrawByRepetition(Position position)
        {
            int hash = (int)(position.Hash() % _prevPositionsCapacity);
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
            int hash = (int)(position.Hash() % _prevPositionsCapacity);
            if(!_previousPositions.ContainsKey(hash))
            {
                _previousPositions.Add(hash, new List<Position>());
            }
            _previousPositions[hash].Add(position);
        }
    }
}
