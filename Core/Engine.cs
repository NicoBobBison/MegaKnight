﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;

namespace MegaKnight.Core
{
    internal class Engine
    {
        // Maximum allowed search time
        public float WhiteTimeRemaining = 1000 * 60;
        public float BlackTimeRemaining = 1000 * 60;
        public float WhiteTimeIncrement = 1000 * 1;
        public float BlackTimeIncrement = 1000 * 1;
        float _maxSearchTime;
        float _engineTimeRemaining;

        // Need to figure out what to do with this. Base it off current depth or always keep constant?
        const int _quiescenceSearchDepth = 7;

        MoveGenerator _moveGenerator;
        Evaluator _evaluator;

        const int _transpositionTableCapacity = 10000000;
        Dictionary<int, TranspositionEntry> _transpositionTable;
        int[,,] _betaCuttoffHistory; // For history heuristic
        Stopwatch _moveStopwatch = Stopwatch.StartNew();
        PVTable _pvTable = new PVTable();
        Move[] _previousPV = null;
        // Should this be a list?
        Move[] _killerMoves = new Move[100];

        #region Info
        int _infoScore = int.MinValue;
        int _infoDepth = 0;
        #endregion

        //int _debugBranchesResearched;
        // int _debugTranspositionsFound;
        //float _debugAveragePlaceOfAlphaCutoff = 0;
        //int _debugNumberPlacesAdded = 0;
        //int _debugBranchesSuccessfullyReduced = 0;

        public Engine(MoveGenerator moveGenerator, Evaluator evaluator)
        {
            _moveGenerator = moveGenerator;
            _evaluator = evaluator;

            _transpositionTable = new Dictionary<int, TranspositionEntry>(_transpositionTableCapacity);
            _betaCuttoffHistory = new int[2, 64, 64];
        }
        public async Task<Move> GetBestMoveAsync(Position position, CancellationToken cancelToken)
        {
            // _debugBranchesResearched = 0;
            _infoScore = int.MinValue;
            _infoDepth = 1;
            // _debugAveragePlaceOfAlphaCutoff = 0;
            // _debugNumberPlacesAdded = 0;
            // _debugBranchesSuccessfullyReduced = 0;

            if (position.WhiteToMove)
            {
                _maxSearchTime = WhiteTimeRemaining / 20 + WhiteTimeIncrement / 2;
            }
            else
            {
                _maxSearchTime = BlackTimeRemaining / 20 + BlackTimeIncrement / 2;
            }
            _engineTimeRemaining = position.WhiteToMove ? WhiteTimeRemaining : BlackTimeRemaining;
            if (position.HashValue == 0) position.InitializeHash();
            _moveStopwatch.Restart();
            _killerMoves = new Move[100];
            // _debugBranchesPruned = 0;

            Move bestMoveSoFar = null;
            int depth = 1;
            await Task.Run(() =>
            {
                while (_moveStopwatch.ElapsedMilliseconds < _maxSearchTime && _engineTimeRemaining - _moveStopwatch.ElapsedMilliseconds > 0 && !cancelToken.IsCancellationRequested)
                {
                    _pvTable = new PVTable();
                    _infoScore = int.MinValue;
                    Move move = Search(position, depth, cancelToken);
                    if (_moveStopwatch.ElapsedMilliseconds < _maxSearchTime && _engineTimeRemaining - _moveStopwatch.ElapsedMilliseconds > 0 && !cancelToken.IsCancellationRequested)
                    {
                        bestMoveSoFar = move;
                        _previousPV = _pvTable.GetPrincipalVariation();
                        Debug.WriteLine("PV Table:");
                        Debug.WriteLine(_pvTable.ToString());

                        Console.WriteLine(GetInfo(_previousPV));
                        depth++;
                        _infoDepth++;
                    }
                }
            }, cancelToken);
            if (position.WhiteToMove)
            {
                WhiteTimeRemaining -= _maxSearchTime;
                WhiteTimeRemaining += WhiteTimeIncrement;
            }
            else
            {
                BlackTimeRemaining -= _maxSearchTime;
                BlackTimeRemaining += BlackTimeIncrement;
            }
            if (bestMoveSoFar == null) throw new Exception("Could not find a move");
            Debug.WriteLine("Engine move: " + bestMoveSoFar.ToString());
            //Debug.WriteLine("Branches pruned: " + _debugBranchesPruned);
            //Debug.WriteLine("Highest base depth searched: " + depth);
            //Debug.WriteLine("Branches researched: " + _debugBranchesResearched);
            //Debug.WriteLine("PV Table:");
            //Debug.WriteLine(_pvTable.ToString());
            //Debug.WriteLine("PV: ");
            //foreach (Move m in _previousPV)
            //{
            //    Debug.Write(" " + m.ToString());
            //}
            Debug.WriteLine("");
            return bestMoveSoFar;
        }

        /// <summary>
        /// Searches from a position using Negamax.
        /// </summary>
        /// <param name="position">The position to start from.</param>
        /// <returns>The best move based on the search.</returns>
        Move Search(Position position, int depth, CancellationToken? cancel = null)
        {
            if (CheckCancel(cancel)) return null;

            if (depth == 0) throw new Exception("Cannot start search with 0 depth");
            // Divide by two to avoid overflow issues
            int alpha = int.MinValue / 2;
            int beta = int.MaxValue / 2;

            int ply = 0;
            int hash = (int)(position.HashValue % _transpositionTableCapacity);
            if (_transpositionTable.ContainsKey(hash) && _transpositionTable[hash].HashKey == position.HashValue && _transpositionTable[hash].Depth >= depth)
            {
                if (_transpositionTable[hash].NodeType == NodeType.LowerBound)
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
            SortMoves(possibleMoves, position, ply);

            List<Move> prevQuietMoves = new List<Move>();
            for (int i = 0; i < possibleMoves.Count; i++)
            {
                Move move = possibleMoves[i];
                position.MakeMove(move);
                _evaluator.AddPositionToPreviousPositions(position);
                int score = -AlphaBeta(position, depth - 1, -beta, -alpha, ply + 1, false, cancel);
                _evaluator.RemovePositionFromPreviousPositions(position);
                position.UnmakeMove(move);
                if (score > max)
                {
                    max = score;
                    _infoScore = score;
                    bestMove = move;
                    if (score > alpha)
                    {
                        alpha = score;
                        if (score < beta)
                        {
                            _pvTable.SetPVValue(move, depth);
                            // Debug.WriteLine("Update PV value: move = " + (move != null ? move.ToString() : " - ") + ", Depth: " + originalDepth + ", Offset: " + (originalDepth - depth));
                        }
                    }
                }
                if (score >= beta)
                {
                    _killerMoves[ply] = bestMove;
                    if (!bestMove.IsCapture() && !bestMove.IsPromotion())
                    {
                        // Add to history (and history malus for moves that haven't caused a beta cutoff)
                        int colorIndex = position.WhiteToMove ? 0 : 1;
                        foreach (Move prev in prevQuietMoves)
                        {
                            _betaCuttoffHistory[colorIndex, Helper.SinglePopBitboardToIndex(prev.StartSquare), Helper.SinglePopBitboardToIndex(prev.EndSquare)] -= depth * depth;
                        }
                        _betaCuttoffHistory[colorIndex, Helper.SinglePopBitboardToIndex(bestMove.StartSquare), Helper.SinglePopBitboardToIndex(bestMove.EndSquare)] += depth * depth;
                    }
                    break;
                }
                if (!move.IsCapture() && !move.IsPromotion())
                {
                    prevQuietMoves.Add(move);
                }
                if (CheckCancel(cancel)) return null;
            }
            AddPositionToTranspositionTable(position, depth, int.MinValue / 2, beta, max, bestMove);
            return bestMove;
        }
        int AlphaBeta(Position position, int depth, int alpha, int beta, int ply, bool nullMoveSearch, CancellationToken? cancel = null)
        {
            if (CheckCancel(cancel)) return -1000000;
            if (_evaluator.IsDrawByRepetition(position)) return 0;

            int alphaOriginal = alpha;
            int hash = (int)(position.HashValue % _transpositionTableCapacity);
            bool isHashMove = false;
            if (_transpositionTable.ContainsKey(hash) && _transpositionTable[hash].HashKey == position.HashValue && _transpositionTable[hash].Depth >= depth)
            {
                if (_transpositionTable[hash].NodeType == NodeType.LowerBound)
                {
                    isHashMove = true;
                    alpha = Math.Max(alpha, _transpositionTable[hash].Evaluation);
                }
                else
                {
                    isHashMove = true;
                    beta = Math.Min(beta, _transpositionTable[hash].Evaluation);
                }
            }

            // We don't flip signs for quiescence search because we aren't going down depth when we call it
            if (depth <= 0) return QuiescenceSearch(position, alpha, beta, _quiescenceSearchDepth, ply, cancel);

            int max = int.MinValue;
            Move bestMove = null;

            List<Move> possibleMoves = _moveGenerator.GenerateAllPossibleMoves(position);
            SortMoves(possibleMoves, position, ply);

            // If we have no legal moves, it's either stalemate or checkmate
            if (possibleMoves.Count == 0)
            {
                if(_moveGenerator.GetPiecesAttackingKing(position) > 0)
                {
                    return -1000000;
                }
                return 0;
            }
            bool kingAttacked = _moveGenerator.GetPiecesAttackingKing(position) != 0;

            // Null move pruning, R = 2
            if (!nullMoveSearch && depth > 2 && !kingAttacked && _evaluator.GetGamePhase(position) < Weights.MiddleGameCutoff)
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
            List<Move> prevNonCaptures = new List<Move>();
            for (int i = 0; i < possibleMoves.Count; i++)
            {
                int score;
                Move move = possibleMoves[i];
                bool isKiller = _killerMoves[ply] == move;
                // Try to reduce the depth of later moves since they are more likely to fail low
                if (depth >= 3 && i > 3 && !isKiller && !isHashMove && !kingAttacked && !move.IsCapture() && !move.IsPromotion())
                {
                    int depthReduction = (int)Math.Max(1, Math.Floor(Math.Log(depth) * Math.Log(i) / 2));
                    position.MakeMove(move);
                    _evaluator.AddPositionToPreviousPositions(position);
                    score = -AlphaBeta(position, depth - 1 - depthReduction, -beta, -beta + 1, ply + 1, nullMoveSearch);
                    _evaluator.RemovePositionFromPreviousPositions(position);
                    position.UnmakeMove(move);
                }
                else
                {
                    score = alpha + 1;
                }
                if (score > alpha)
                {
                    position.MakeMove(move);
                    _evaluator.AddPositionToPreviousPositions(position);
                    score = -AlphaBeta(position, depth - 1, -beta, -alpha, ply + 1, nullMoveSearch);
                    _evaluator.RemovePositionFromPreviousPositions(position);
                    position.UnmakeMove(move);
                    if (score > max)
                    {
                        max = score;
                        bestMove = move;
                        if (score > alpha)
                        {
                            alpha = score;
                            if(score < beta)
                            {
                                _pvTable.SetPVValue(move, depth);
                                // Debug.WriteLine("Update PV value: move = " + (move != null ? move.ToString() : " - ") + ", Depth: " + originalDepth + ", Offset: " + (originalDepth - depth));
                            }
                        }
                    }
                    if (score >= beta)
                    {
                        _killerMoves[ply] = bestMove;
                        if (!bestMove.IsCapture() && !bestMove.IsPromotion())
                        {
                            // Add to history
                            int colorIndex = position.WhiteToMove ? 0 : 1;
                            foreach(Move prev in prevNonCaptures)
                            {
                                _betaCuttoffHistory[colorIndex, Helper.SinglePopBitboardToIndex(prev.StartSquare), Helper.SinglePopBitboardToIndex(prev.EndSquare)] -= depth * depth;
                            }
                            _betaCuttoffHistory[colorIndex, Helper.SinglePopBitboardToIndex(bestMove.StartSquare), Helper.SinglePopBitboardToIndex(bestMove.EndSquare)] += depth * depth;
                        }
                        break;
                    }
                }
                if (!move.IsCapture() && !move.IsPromotion())
                {
                    prevNonCaptures.Add(move);
                }
                if (CheckCancel(cancel)) return -1000000;
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

        int QuiescenceSearch(Position position, int alpha, int beta, int depth, int ply, CancellationToken? cancel = null)
        {
            if (CheckCancel(cancel)) return -1000000;

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
            SortMoves(moves, position, ply);

            // Delta pruning, subtract 200 centipawns as a safety net (do we need to also add for pawn promotion?)
            int queenVal = (int)Helper.Lerp(Weights.QueenValueEarly, Weights.QueenValueLate, _evaluator.GetGamePhase(position));
            int bigDelta = queenVal - 200;
            foreach (Move m in moves)
            {
                if (m.IsPromotion())
                {
                    bigDelta += queenVal;
                }
            }
            // Check if best possible material capture would save position (also check game phase since we don't want this in endgame)
            if (standingPat + bigDelta < alpha && _evaluator.GetGamePhase(position) < Weights.MiddleGameCutoff)
            {
                return standingPat;
            }

            foreach (Move move in moves)
            {
                if (!move.IsCapture()) continue;
                position.MakeMove(move);
                _evaluator.AddPositionToPreviousPositions(position);
                int score = -QuiescenceSearch(position, -beta, -alpha, depth - 1, ply + 1);
                _evaluator.RemovePositionFromPreviousPositions(position);
                position.UnmakeMove(move);
                if (score > max)
                {
                    max = score;
                    alpha = Math.Max(alpha, score);
                }
                if (score >= beta) break;
                if (CheckCancel(cancel)) return -1000000;
            }
            return max;
        }
        public string GetInfo(Move[] pv)
        {
            string info = "info ";
            info += $"depth {_infoDepth} ";
            info += $"score cp {_infoScore} ";
            // Reimplement this once PV always displays correctly
            if (pv.Length > 0)
            {
                info += "pv";
                foreach (Move m in pv)
                {
                    if (m == null) break;
                    info += " " + m.ToString();
                }
            }
            return info;
        }
        private bool CheckCancel(CancellationToken? cancel)
        {
            return (_moveStopwatch.ElapsedMilliseconds >= _maxSearchTime || _engineTimeRemaining - _moveStopwatch.ElapsedMilliseconds <= 0 ||
               (cancel.HasValue && cancel.Value.IsCancellationRequested));
        }
        void SortMoves(List<Move> moves, Position position, int ply)
        {
            // Sort moves with comparer. See MoveComparer for sorting methods
            MoveComparer comparer = new MoveComparer(_transpositionTable, _transpositionTableCapacity, position, _previousPV, _killerMoves, ply, _betaCuttoffHistory);
            moves.Sort(comparer);
            // Debug.WriteLine(string.Join(' ', moves.Select(x => x.DetailedToString(position))) + "\n");
        }
        private static void PutMoveAtIndexInList(List<Move> moves, int indexToRemove, int indexToInsertAt)
        {
            Move m = moves[indexToRemove];
            moves.RemoveAt(indexToRemove);
            moves.Insert(indexToInsertAt, m);
        }
    }
}
