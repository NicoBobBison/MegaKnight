using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MegaKnight.Core
{
    // Should quiet moves be searched before losing captures?
    /// <summary>
    /// Sorts moves. Move ordering is as follows.
    /// 1. PV move
    /// 2. Hash move
    /// 3. Winning captures
    /// 4. Equal captures
    /// 5. Killer moves
    /// 6. Quiet moves (sorted with history heuristic)
    /// 7. Losing captures
    /// </summary>
    internal class MoveComparer : IComparer<Move>
    {
        Dictionary<int, TranspositionEntry> _transpositionTable;
        int _transpositionTableCapacity;
        Move[] _prevPV;
        Position _position;
        Move[] _killerMoves;
        int _ply;
        int[,,] _history;
        public MoveComparer(Dictionary<int, TranspositionEntry> transpositionTable, int ttCapacity, Position position, Move[] prevPV, Move[] killerMoves, int ply, int[,,] history)
        {
            _transpositionTable = transpositionTable;
            _transpositionTableCapacity = ttCapacity;
            _prevPV = prevPV;
            _position = position;
            _killerMoves = killerMoves;
            _ply = ply;
            _history = history;
        }
        public int Compare(Move x, Move y)
        {
            if (x.Equals(y)) return 0;

            int hash = (int)(_position.HashValue % (uint)_transpositionTableCapacity);

            // PV from previous iterative deepening search
            if (_prevPV != null && _prevPV.Length > _ply)
            {
                if (_prevPV[_ply].Equals(x))
                {
                    return -2000;
                }
                else if (_prevPV[_ply].Equals(y))
                {
                    return 2000;
                }
            }

            // Hash move
            if(_transpositionTable.ContainsKey(hash) && (int)(_transpositionTable[hash].HashKey % (uint)_transpositionTableCapacity) == hash)
            {
                if (_transpositionTable[hash].BestMove != null)
                {
                    if (_transpositionTable[hash].BestMove.Equals(x))
                    {
                        return -1000;
                    }
                    if (_transpositionTable[hash].BestMove.Equals(y))
                    {
                        return 1000;
                    }
                }
            }

            bool xIsCapture = x.IsCapture();
            bool yIsCapture = y.IsCapture();

            int[] pieceCapturingValues = new int[] { 1, 3, 3, 5, 9, 20 };
            // Defaults to very low value
            int xVictimMinusAttacker = -1000;
            int yVictimMinusAttacker = -1000;

            // Sets MVV/LVA value for captures
            if (xIsCapture)
            {
                xVictimMinusAttacker = pieceCapturingValues[(int)x.GetPieceCapturing(_position)] - pieceCapturingValues[(int)x.Piece];
            }
            if (yIsCapture)
            {
                yVictimMinusAttacker = pieceCapturingValues[(int)y.GetPieceCapturing(_position)] - pieceCapturingValues[(int)y.Piece];
            }

            // Winning captures
            if (xVictimMinusAttacker > 0 && xVictimMinusAttacker > yVictimMinusAttacker) return -800 - xVictimMinusAttacker;
            if (yVictimMinusAttacker > 0 && yVictimMinusAttacker > xVictimMinusAttacker) return 800 + yVictimMinusAttacker;

            // Equal captures
            if (xVictimMinusAttacker == 0 && xVictimMinusAttacker > yVictimMinusAttacker) return -700 - xVictimMinusAttacker;
            if (yVictimMinusAttacker == 0 && yVictimMinusAttacker > xVictimMinusAttacker) return 700 + yVictimMinusAttacker;

            // Killer moves
            if (_killerMoves[_ply] != null)
            {
                if (_killerMoves[_ply].Equals(x))
                {
                    return -700;
                }
                if (_killerMoves[_ply].Equals(y))
                {
                    return 700;
                }
            }

            int xStartIndex = Helper.SinglePopBitboardToIndex(x.StartSquare);
            int yStartIndex = Helper.SinglePopBitboardToIndex(y.StartSquare);
            int xEndIndex = Helper.SinglePopBitboardToIndex(x.EndSquare);
            int yEndIndex = Helper.SinglePopBitboardToIndex(y.EndSquare);

            // Quiet moves (sorted by history heuristic)
            if(xVictimMinusAttacker == yVictimMinusAttacker)
            {
                if (_history[_position.WhiteToMove ? 0 : 1, xStartIndex, xEndIndex] > _history[_position.WhiteToMove ? 0 : 1, yStartIndex, yEndIndex])
                {
                    return -400;
                }
                else if (_history[_position.WhiteToMove ? 0 : 1, xStartIndex, xEndIndex] < _history[_position.WhiteToMove ? 0 : 1, yStartIndex, yEndIndex])
                {
                    return 400;
                }
            }

            // Losing captures
            if (xVictimMinusAttacker < 0 && xVictimMinusAttacker > yVictimMinusAttacker) return -200 - xVictimMinusAttacker;
            if (yVictimMinusAttacker < 0 && yVictimMinusAttacker > xVictimMinusAttacker) return 200 + yVictimMinusAttacker;

            return 0;
        }
    }
}
