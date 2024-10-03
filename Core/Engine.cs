using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MegaKnight.Core
{
    internal class Engine
    {
        MoveGenerator _moveGenerator;
        Evaluator _evaluator;
        public Engine(MoveGenerator moveGenerator, Evaluator evaluator)
        {
            _moveGenerator = moveGenerator;
            _evaluator = evaluator;
        }
        public Move GetBestMove(Position position)
        {
            Random r = new Random();
            List<Move> moves = _moveGenerator.GenerateAllPossibleMoves(position);
            return moves[r.Next(0, moves.Count)];
        }
        
    }
}
