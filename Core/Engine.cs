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
        const int defaultSearchDepth = 4;

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

            // Divide by two to avoid overflow issues
            int alpha = int.MinValue / 2;
            int beta = int.MaxValue / 2;

            int max = int.MinValue;
            Move bestMove = null;

            foreach (Move move in _moveGenerator.GenerateAllPossibleMoves(position))
            {
                position.MakeMove(move);
                int score = -AlphaBeta(position, depth - 1, -beta, -alpha);
                position.UnmakeMove(move);
                if (score > max)
                {
                    max = score;
                    bestMove = move;
                    alpha = Math.Max(alpha, score);
                }
                if (score >= beta) return bestMove;
            }
            // Debug.WriteLine("Best move score: " + max);
            return bestMove;
        }
        int AlphaBeta(Position position, int depth, int alpha, int beta)
        {
            // TODO: Add quiescence search
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
                int score = -AlphaBeta(position, depth - 1, -beta, -alpha);
                position.UnmakeMove(move);
                if (score > max)
                {
                    max = score;
                    alpha = Math.Max(alpha, score);
                }
                if (score >= beta) return score;
            }
            return max;
        }
    }
}
