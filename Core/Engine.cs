using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace MegaKnight.Core
{
    internal class Engine
    {
        // Maximum allowed search time
        const float _maxSearchTime = 10;

        // Need to figure out what to do with this. Base it off current depth or always keep constant?
        const int _quiescenceSearchDepth = 2;

        MoveGenerator _moveGenerator;
        Evaluator _evaluator;

        const int _transpositionTableCapacity = 1000000;
        Dictionary<int, TranspositionEntry> _transpositionTable;
        Stopwatch _moveStopwatch = Stopwatch.StartNew();

        int _debugBranchesPruned;

        public Engine(MoveGenerator moveGenerator, Evaluator evaluator)
        {
            _moveGenerator = moveGenerator;
            _evaluator = evaluator;

            _transpositionTable = new Dictionary<int, TranspositionEntry>(_transpositionTableCapacity);
        }
        public Move GetBestMove(Position position)
        {
            _moveStopwatch.Restart();
            Move bestMoveSoFar = null;
            int depth = 1;
            while(_moveStopwatch.ElapsedMilliseconds / 1000 < _maxSearchTime)
            {
                Move move = Search(position, depth);
                if (_moveStopwatch.ElapsedMilliseconds / 1000 < _maxSearchTime)
                {
                    bestMoveSoFar = move;
                    depth++;
                }
            }
            if (bestMoveSoFar == null) throw new Exception("Could not find a move fast enough");
            return bestMoveSoFar;
        }
        /// <summary>
        /// Searches from a position using Negamax.
        /// </summary>
        /// <param name="position">The position to start from.</param>
        /// <returns>The best move based on the search.</returns>
        Move Search(Position position, int depth)
        {
            if (depth == 0) throw new Exception("Cannot start search with 0 depth");
            _debugBranchesPruned = 0;

            // Divide by two to avoid overflow issues
            int alpha = int.MinValue / 2;
            int beta = int.MaxValue / 2;

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
                if (score >= beta)
                {
                    _debugBranchesPruned++;
                    break;
                }
            }
            AddPositionToTranspositionTable(position, depth, int.MinValue / 2, beta, max, bestMove);
            Debug.WriteLine("Engine move: " + bestMove.ToString());
            // Debug.WriteLine("Branches pruned: " + _debugBranchesPruned);
            Debug.WriteLine("Depth this search: " + depth);
            return bestMove;
        }
        int AlphaBeta(Position position, int depth, int alpha, int beta)
        {
            if (_moveStopwatch.ElapsedMilliseconds / 1000 >= _maxSearchTime) return 0;
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
            if (depth == 0) return QuiescenceSearch(position, alpha, beta, _quiescenceSearchDepth);

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
                if (score >= beta)
                {
                    _debugBranchesPruned++;
                    break;
                }
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
            // "Always replace" strategy
            _transpositionTable[(int)(entry.HashKey % _transpositionTableCapacity)] = entry;
        }

        int QuiescenceSearch(Position position, int alpha, int beta, int depth)
        {
            if (_moveStopwatch.ElapsedMilliseconds / 1000 >= _maxSearchTime) return 0;
            // The lower bound for moves we can make from this position
            int standingPat = _evaluator.Evaluate(position);
            if (standingPat >= beta || depth == 0) return standingPat;
            alpha = Math.Max(alpha, standingPat);

            int max = standingPat;
            List<Move> moves = _moveGenerator.GenerateAllPossibleMoves(position);
            SortMoves(moves, position);
            foreach (Move move in moves)
            {
                if (!move.IsCapture()) continue;
                position.MakeMove(move);
                int score = -QuiescenceSearch(position, -beta, -alpha, depth - 1);
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
            for (int i = insertPos; i < moves.Count; i++)
            {
                if (_transpositionTable.ContainsKey(hash) && _transpositionTable[hash].BestMove == moves[i])
                {
                    PutMoveAtIndexInList(moves, i, insertPos);
                    insertPos++;
                }
            }

            // Check captures
            for (int i = insertPos; i < moves.Count; i++)
            {
                if (moves[i].IsCapture())
                {
                    PutMoveAtIndexInList(moves, i, insertPos);
                    insertPos++;
                }
            }
        }
        private static void PutMoveAtIndexInList(List<Move> moves, int indexToRemove, int indexToInsertAt)
        {
            Move m = moves[indexToRemove];
            moves.RemoveAt(indexToRemove);
            moves.Insert(indexToInsertAt, m);
        }
    }
}
