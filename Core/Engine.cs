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

        const int _transpositionTableCapacity = 10000;
        Dictionary<int, TranspositionEntry> _transpositionTable;

        public Engine(MoveGenerator moveGenerator, Evaluator evaluator)
        {
            _moveGenerator = moveGenerator;
            _evaluator = evaluator;

            _transpositionTable = new Dictionary<int, TranspositionEntry>(_transpositionTableCapacity);
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

            int alphaOriginal = alpha;
            int hash = (int)(position.Hash() % _transpositionTableCapacity);
            if (_transpositionTable.ContainsKey(hash) && _transpositionTable[hash].HashKey == position.Hash() && _transpositionTable[hash].Depth >= depth)
            {
                if (_transpositionTable[hash].NodeType == NodeType.PVNode)
                {
                    return _transpositionTable[hash].BestMove;
                }
                else if (_transpositionTable[hash].NodeType == NodeType.CutNode)
                {
                    alpha = Math.Max(alpha, _transpositionTable[hash].Evaluation);
                }
                else
                {
                    beta = Math.Min(beta, _transpositionTable[hash].Evaluation);
                }
            }

            int max = int.MinValue;
            Move bestMove = null;

            List<Move> possibleMoves = _moveGenerator.GenerateAllPossibleMoves(position);
            SortMoves(possibleMoves, position);

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
                if (score >= beta) break;
            }
            // Debug.WriteLine("Best move score: " + max);
            AddPositionToTranspositionTable(position, depth, int.MinValue / 2, int.MaxValue / 2, max, bestMove);
            Debug.WriteLine("Engine move: " + bestMove.ToString());
            return bestMove;
        }
        int AlphaBeta(Position position, int depth, int alpha, int beta)
        {
            int alphaOriginal = alpha;
            int hash = (int)(position.Hash() % _transpositionTableCapacity);
            if (_transpositionTable.ContainsKey(hash) && _transpositionTable[hash].HashKey == position.Hash() && _transpositionTable[hash].Depth >= depth)
            {
                if (_transpositionTable[hash].NodeType == NodeType.PVNode)
                {
                    return _transpositionTable[hash].Evaluation;
                }
                else if (_transpositionTable[hash].NodeType == NodeType.CutNode)
                {
                    alpha = Math.Max(alpha, _transpositionTable[hash].Evaluation);
                }
                else
                {
                    beta = Math.Min(beta, _transpositionTable[hash].Evaluation);
                }
            }

            // We don't flip signs for quiescence search because we aren't going down depth when we call it
            if (depth == 0) return QuiescenceSearch(position, alpha, beta);

            int max = int.MinValue;
            Move bestMove = null;

            List<Move> possibleMoves = _moveGenerator.GenerateAllPossibleMoves(position);
            SortMoves(possibleMoves, position);

            // If we have no legal moves, it's either stalemate or checkmate
            if (possibleMoves.Count == 0)
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
                    bestMove = move;
                    alpha = Math.Max(alpha, score);
                }
                if (score >= beta) break;
            }
            AddPositionToTranspositionTable(position, depth, alphaOriginal, beta, max, bestMove);
            return max;
        }
        private void AddPositionToTranspositionTable(Position position, int depth, int alpha, int beta, int max, Move bestMove)
        {
            TranspositionEntry entry = new TranspositionEntry();
            entry.Depth = depth;
            entry.Evaluation = max;
            entry.BestMove = bestMove;
            entry.HashKey = position.Hash();
            if (max <= alpha)
            {
                entry.NodeType = NodeType.AllNode;
            }
            else if (max >= beta)
            {
                entry.NodeType = NodeType.CutNode;
            }
            else
            {
                entry.NodeType = NodeType.PVNode;
            }
            _transpositionTable[(int)(entry.HashKey % _transpositionTableCapacity)] = entry;
        }

        int QuiescenceSearch(Position position, int alpha, int beta)
        {
            // The lower bound for moves we can make from this position
            int standingPat = _evaluator.Evaluate(position);
            if (standingPat >= beta) return standingPat;
            alpha = Math.Max(alpha, standingPat);

            int max = standingPat;
            List<Move> moves = _moveGenerator.GenerateAllPossibleMoves(position);
            SortMoves(moves, position);
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
                if(score >= beta) break;
            }
            return max;
        }
        void SortMoves(List<Move> moves, Position position)
        {
            int insertPos = 0;

            int hash = (int)(position.Hash() % _transpositionTableCapacity);
            // Search for hash move first
            for(int i = 0; i < moves.Count; i++)
            {
                if (_transpositionTable.ContainsKey(hash) && _transpositionTable[hash].BestMove == moves[i])
                {
                    PutMoveAtIndexInList(moves, i);
                    insertPos++;
                }
            }

            // Check captures
            for (int i = 0; i < moves.Count; i++)
            {
                if (moves[i].IsCapture())
                {
                    PutMoveAtIndexInList(moves, i);
                    insertPos++;
                }
            }
        }

        private static void PutMoveAtIndexInList(List<Move> moves, int indexToRemove)
        {
            Move m = moves[indexToRemove];
            moves.RemoveAt(indexToRemove);
            moves.Insert(0, m);
        }
    }
}
