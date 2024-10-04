using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MegaKnight.Core
{
    internal class Engine
    {
        const int defaultSearchDepth = 3;

        MoveGenerator _moveGenerator;
        Evaluator _evaluator;
        public Engine(MoveGenerator moveGenerator, Evaluator evaluator)
        {
            _moveGenerator = moveGenerator;
            _evaluator = evaluator;
        }
        public Move GetBestMove(Position position)
        {
            return Search(position, defaultSearchDepth);
        }
        /// <summary>
        /// Searches from a position using Negamax.
        /// </summary>
        /// <param name="position">The position to start from.</param>
        /// <returns>The best move based on the search.</returns>
        Move Search(Position position, int depth)
        {
            if (depth == 0) throw new Exception("Cannot start search with 0 depth");

            int max = int.MinValue;
            Move bestMove = null;

            foreach (Move move in _moveGenerator.GenerateAllPossibleMoves(position))
            {
                position.MakeMove(move);
                int score = -SearchRecursive(position, depth - 1);
                position.UnmakeMove(move);
                if (score > max)
                {
                    max = score;
                    bestMove = move;
                }
            }
            // Debug.WriteLine("Best move score: " + max);
            return bestMove;
        }
        int SearchRecursive(Position position, int depth)
        {
            if(depth == 0) return _evaluator.Evaluate(position);
            int max = int.MinValue;
            List<Move> possibleMoves = _moveGenerator.GenerateAllPossibleMoves(position);
            // If we have no legal moves, it's either stalemate or checkmate
            if(possibleMoves.Count == 0)
            {
                return _evaluator.Evaluate(position);
            }
            foreach (Move move in possibleMoves)
            {
                position.MakeMove(move);
                int score = -SearchRecursive(position, depth - 1);
                position.UnmakeMove(move);
                if (score > max)
                {
                    max = score;
                }
            }
            return max;
        }
    }
}
