using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessBot.Core
{
    internal class PositionEvaluator
    {
        MoveGenerator _moveGenerator;
        BotCore _core;
        public PositionEvaluator(MoveGenerator moveGenerator, BotCore core)
        {
            _moveGenerator = moveGenerator;
            _core = core;
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
    }
}
