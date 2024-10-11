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
        // Maximum allowed search time
        const float _maxSearchTime = 3f;

        // Need to figure out what to do with this. Base it off current depth or always keep constant?
        const int _quiescenceSearchDepth = 3;

        MoveGenerator _moveGenerator;
        Evaluator _evaluator;

        const int _transpositionTableCapacity = 10000000;
        Dictionary<int, TranspositionEntry> _transpositionTable;
        Stopwatch _moveStopwatch = Stopwatch.StartNew();
        //PVTable _principalVariation = new PVTable();
        // Should this be a list?
        Move[] _killerMoves = new Move[100];

        int _debugBranchesPruned;
        int _debugTranspositionsFound;

        public Engine(MoveGenerator moveGenerator, Evaluator evaluator)
        {
            _moveGenerator = moveGenerator;
            _evaluator = evaluator;

            _transpositionTable = new Dictionary<int, TranspositionEntry>(_transpositionTableCapacity);
        }
        public Move GetBestMove(Position position)
        {
            if(position.HashValue == 0) position.InitializeHash();
            _moveStopwatch.Restart();
            _killerMoves = new Move[100];

            //_principalVariation = new PVTable();
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
            if (bestMoveSoFar == null) throw new Exception("Could not find a move");
            Debug.WriteLine("Engine move: " + bestMoveSoFar.ToString());
            //Debug.WriteLine("Branches pruned: " + _debugBranchesPruned);
            Debug.WriteLine("Highest base depth searched: " + depth);
            //Debug.Write("PV: ");
            //foreach(Move m in CollectPV(position))
            //{
            //    Debug.Write(m.ToString() + " ");
            //}
            //Debug.WriteLine("Principle variation table: ");
            //Debug.WriteLine(_principalVariation.ToString());
            //Debug.Write("Principle variation: ");
            //foreach(Move m in _principalVariation.GetPrincipalVariation())
            //{
            //    if (m == null) Debug.Write("- ");
            //    else Debug.Write(m.ToString() + " ");
            //}
            Debug.WriteLine("");
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

            int ply = 0;
            int hash = (int)(position.HashValue % _transpositionTableCapacity);
            if (_transpositionTable.ContainsKey(hash) && _transpositionTable[hash].HashKey == position.HashValue && _transpositionTable[hash].Depth >= depth)
            {

                if (_transpositionTable[hash].NodeType == NodeType.Exact)
                {
                    return _transpositionTable[hash].BestMove;
                }
                else if (_transpositionTable[hash].NodeType == NodeType.LowerBound)
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
            SortMoves(possibleMoves, position, depth);

            foreach (Move move in possibleMoves)
            {
                position.MakeMove(move);
                int score = -AlphaBeta(position, depth - 1, -beta, -alpha, ply + 1, false);
                position.UnmakeMove(move);
                if (score > max)
                {
                    max = score;
                    bestMove = move;
                    if (score > alpha)
                    {
                        alpha = score;
                        //_principalVariation.SetPVValue(move, ply + depth, ply);
                        // Debug.WriteLine("Update PV value: move = " + (move != null ? move.ToString() : " - ") + ", Depth: " + originalDepth + ", Offset: " + (originalDepth - depth));
                    }
                }
                if (score >= beta)
                {
                    _killerMoves[depth] = bestMove;
                    break;
                }
            }
            AddPositionToTranspositionTable(position, depth, int.MinValue / 2, beta, max, bestMove);
            return bestMove;
        }
        int AlphaBeta(Position position, int depth, int alpha, int beta, int ply, bool nullMoveSearch)
        {
            int alphaOriginal = alpha;
            if (_moveStopwatch.ElapsedMilliseconds / 1000 >= _maxSearchTime) return 0;
            int hash = (int)(position.HashValue % _transpositionTableCapacity);
            if (_transpositionTable.ContainsKey(hash) && _transpositionTable[hash].HashKey == position.HashValue && _transpositionTable[hash].Depth >= depth)
            {
                if (_transpositionTable[hash].NodeType == NodeType.Exact)
                {
                    return _transpositionTable[hash].Evaluation;
                }
                else if (_transpositionTable[hash].NodeType == NodeType.LowerBound)
                {
                    alpha = Math.Max(alpha, _transpositionTable[hash].Evaluation);
                }
                else
                {
                    beta = Math.Min(beta, _transpositionTable[hash].Evaluation);
                }
            }

            // We don't flip signs for quiescence search because we aren't going down depth when we call it
            if (depth <= 0) return QuiescenceSearch(position, alpha, beta, _quiescenceSearchDepth);

            int max = int.MinValue;
            Move bestMove = null;

            List<Move> possibleMoves = _moveGenerator.GenerateAllPossibleMoves(position);
            SortMoves(possibleMoves, position, depth);

            // If we have no legal moves, it's either stalemate or checkmate
            if (possibleMoves.Count == 0)
            {
                return _evaluator.Evaluate(position);
            }
            // Null move pruning, R = 2
            if (!nullMoveSearch && depth > 2 && _moveGenerator.GetPiecesAttackingKing(position) == 0)
            {
                position.MakeNullMove();
                int nullMoveScore = -AlphaBeta(position, depth - 1 - 2, -beta, -beta + 1, ply + 1, true);
                position.UnmakeNullMove();
                // Prune this branch if the position is so strong that skipping a turn would still result in a winning position
                if (nullMoveScore >= beta)
                {
                    return nullMoveScore;
                }
            }
            foreach (Move move in possibleMoves)
            {
                ulong hashBefore = position.HashValue;
                position.MakeMove(move);
                //Debug.WriteLine(new string('\t', originalDepth - depth) + "M: " + move.ToString());
                int score = -AlphaBeta(position, depth - 1, -beta, -alpha, ply + 1, nullMoveSearch);
                position.UnmakeMove(move);
                if(hashBefore != position.HashValue)
                {
                    // Bug happens whenever double pawn push is made, not undoing the last en passant hash
                    Debug.WriteLine(position.ToString());
                    Debug.WriteLine(move.ToString());
                    throw new Exception("Making and unmaking move caused hash mismatch");
                }
                //Debug.WriteLine(new string('\t', originalDepth - depth) + "U: " + move.ToString());
                if (score > max)
                {
                    max = score;
                    bestMove = move;
                    if(score > alpha)
                    {
                        alpha = score;
                        //_principalVariation.SetPVValue(move, ply + depth, ply);
                        // Debug.WriteLine("Update PV value: move = " + (move != null ? move.ToString() : " - ") + ", Depth: " + originalDepth + ", Offset: " + (originalDepth - depth));
                    }
                }
                if (score >= beta)
                {
                    _killerMoves[depth] = bestMove;
                    break;
                }
            }
            AddPositionToTranspositionTable(position, depth, alphaOriginal, beta, max, bestMove);
            return max;
        }
        private void AddPositionToTranspositionTable(Position position, int depth, int alpha, int beta, int evaluation, Move bestMove)
        {
            TranspositionEntry entry = new TranspositionEntry();
            entry.Depth = depth;
            entry.Evaluation = evaluation;
            entry.HashKey = position.HashValue;
            if (evaluation <= alpha)
            {
                entry.NodeType = NodeType.UpperBound;
            }
            else if (evaluation >= beta)
            {
                entry.NodeType = NodeType.LowerBound;
            }
            else
            {
                entry.NodeType = NodeType.Exact;
                entry.BestMove = bestMove;
            }
            // "Always replace" strategy
            _transpositionTable[(int)(entry.HashKey % _transpositionTableCapacity)] = entry;
        }

        int QuiescenceSearch(Position position, int alpha, int beta, int depth)
        {
            if (_moveStopwatch.ElapsedMilliseconds / 1000 >= _maxSearchTime) return 0;
            int standingPat = _evaluator.Evaluate(position);
            if (standingPat >= beta || depth == 0) return standingPat;
            int hash = (int)(position.HashValue % _transpositionTableCapacity);
            if (_transpositionTable.ContainsKey(hash) && _transpositionTable[hash].HashKey == position.HashValue && _transpositionTable[hash].Depth >= depth)
            {
                if (_transpositionTable[hash].NodeType == NodeType.Exact)
                {
                    return _transpositionTable[hash].Evaluation;
                }
                else if (_transpositionTable[hash].NodeType == NodeType.LowerBound)
                {
                    alpha = Math.Max(alpha, _transpositionTable[hash].Evaluation);
                }
                else
                {
                    beta = Math.Min(beta, _transpositionTable[hash].Evaluation);
                }
            }
            // The lower bound for moves we can make from this position
            alpha = Math.Max(alpha, standingPat);

            int max = standingPat;
            List<Move> moves = _moveGenerator.GenerateAllPossibleMoves(position);
            SortMoves(moves, position, depth);
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
        List<Move> CollectPV(Position startPosition)
        {
            List<Move> pv = new List<Move>();
            CollectPVRecursive(pv, startPosition);
            return pv;
        }
        void CollectPVRecursive(List<Move> pv, Position position)
        {
            int hash = (int)(position.HashValue % _transpositionTableCapacity);
            if (!_transpositionTable.ContainsKey(hash)) return;

            Move bestMove = _transpositionTable[hash].BestMove;
            if (bestMove == null) return;

            pv.Add(bestMove);
            position.MakeMove(bestMove);
            CollectPVRecursive(pv, position);
            position.UnmakeMove(bestMove);
        }
        void SortMoves(List<Move> moves, Position position, int depth)
        {
            // Sort moves with comparer. See MoveComparer for sorting methods
            MoveComparer comparer = new MoveComparer(_transpositionTable, _transpositionTableCapacity, position, _killerMoves, depth);
            moves.Sort(comparer);
        }
        private static void PutMoveAtIndexInList(List<Move> moves, int indexToRemove, int indexToInsertAt)
        {
            Move m = moves[indexToRemove];
            moves.RemoveAt(indexToRemove);
            moves.Insert(indexToInsertAt, m);
        }
    }
}
