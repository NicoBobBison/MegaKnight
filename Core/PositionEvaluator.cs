using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MegaKnight.Core
{
    internal class PositionEvaluator
    {
        MoveGenerator _moveGenerator;
        BotCore _core;
        const int _prevPositionsCapacity = 500;
        Dictionary<int, List<Position>> _previousPositions;
        public PositionEvaluator(MoveGenerator moveGenerator, BotCore core)
        {
            _moveGenerator = moveGenerator;
            _core = core;
            _previousPositions = new Dictionary<int, List<Position>>(_prevPositionsCapacity);
        }
        public bool IsCheckmate(Position position)
        {
            // If we're not in check, it's not checkmate
            if (_moveGenerator.GetPiecesAttackingKing(position) == 0) return false;

            List<Move> allMoves = _moveGenerator.GenerateAllPossibleMoves(position);
            foreach (Move move in allMoves)
            {
                Position p = (Position)_core.CurrentPosition.Clone();
                p = _core.MakeMove(move, p);
                if(_moveGenerator.GetPiecesAttackingKing(p) == 0) return false;
            }
            // Every move still leaves the king in check
            return true;
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
