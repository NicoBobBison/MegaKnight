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
    /// 1. Hash move
    /// 2. Winning captures
    /// 3. Equal captures
    /// 4. Killer moves
    /// 5. Losing captures
    /// 6. Quiet moves
    /// </summary>
    internal class MoveComparer : IComparer<Move>
    {
        Dictionary<int, TranspositionEntry> _transpositionTable;
        int _transpositionTableCapacity;
        Position _position;
        Move[] _killerMoves;
        int _ply;
        public MoveComparer(Dictionary<int, TranspositionEntry> transpositionTable, int ttCapacity, Position position, Move[] killerMoves, int ply)
        {
            _transpositionTable = transpositionTable;
            _transpositionTableCapacity = ttCapacity;
            _position = position;
            _killerMoves = killerMoves;
            _ply = ply;
        }
        public int Compare(Move x, Move y)
        {
            if (x.Equals(y)) return 0;

            int hash = (int)(_position.HashValue % (uint)_transpositionTableCapacity);

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

            // Winning captures, then equal captures
            if (xIsCapture && yIsCapture)
            {
                int[] pieceCapturingValues = new int[] { 1, 3, 3, 5, 9, 20 };
                int xVictimMinusAttacker = pieceCapturingValues[(int)x.GetPieceCapturing(_position)] - pieceCapturingValues[(int)x.Piece];
                int yVictimMinusAttacker = pieceCapturingValues[(int)y.GetPieceCapturing(_position)] - pieceCapturingValues[(int)y.Piece];
                if (xVictimMinusAttacker >= 0 && xVictimMinusAttacker > yVictimMinusAttacker) return -800 - xVictimMinusAttacker;
                if (yVictimMinusAttacker >= 0 && yVictimMinusAttacker > xVictimMinusAttacker) return 800 + yVictimMinusAttacker;
                if (xVictimMinusAttacker < 0 && xVictimMinusAttacker > yVictimMinusAttacker) return -600 - xVictimMinusAttacker;
                if (yVictimMinusAttacker < 0 && yVictimMinusAttacker > xVictimMinusAttacker) return 600 + yVictimMinusAttacker;
            }
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
            // Losing captures
            if (xIsCapture && !yIsCapture)
            {
                return -500;
            }
            if (yIsCapture && !xIsCapture)
            {
                return 500;
            }

            return 0;
        }
    }
}
