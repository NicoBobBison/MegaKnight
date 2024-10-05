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
        const int defaultSearchDepth = 5;

        MoveGenerator _moveGenerator;
        Evaluator _evaluator;
        public Engine(MoveGenerator moveGenerator, Evaluator evaluator)
        {
            _moveGenerator = moveGenerator;
            _evaluator = evaluator;

            List<Move> moves = new List<Move>();
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

            List<Move> possibleMoves = _moveGenerator.GenerateAllPossibleMoves(position);
            SortMoves(possibleMoves);

            foreach (Move move in possibleMoves)
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
            // We don't flip signs for quiescence search because we aren't going down depth when we call it
            if(depth == 0) return QuiescenceSearch(position, alpha, beta);

            int max = int.MinValue;
            List<Move> possibleMoves = _moveGenerator.GenerateAllPossibleMoves(position);
            SortMoves(possibleMoves);

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
        int QuiescenceSearch(Position position, int alpha, int beta)
        {
            // The lower bound for moves we can make from this position
            int standingPat = _evaluator.Evaluate(position);
            if (standingPat >= beta) return standingPat;
            alpha = Math.Max(alpha, standingPat);

            int max = standingPat;
            List<Move> moves = _moveGenerator.GenerateAllPossibleMoves(position);
            SortMoves(moves);
            foreach (Move move in moves)
            {
                if (!move.IsCapture()) continue;
                position.MakeMove(move);
                int score = -QuiescenceSearch(position, -beta, -alpha);
                position.UnmakeMove(move);
                if(score > max)
                {
                    max = score;
                    alpha = Math.Max(alpha, score);
                }
                if(score >= beta) return score;
            }
            return max;
        }
        void SortMoves(List<Move> moves)
        {
            // TODO: Add better ways of sorting moves
            for(int i = 0; i < moves.Count; i++)
            {
                // Check captures first
                if (moves[i].IsCapture())
                {
                    Move m = moves[i];
                    moves.RemoveAt(i);
                    moves.Insert(0, m);
                }
            }
        }
    }
}
