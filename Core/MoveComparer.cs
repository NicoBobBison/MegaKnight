using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MegaKnight.Core
{
    internal class MoveComparer : IComparer<Move>
    {
        Dictionary<int, TranspositionEntry> _transpositionTable;
        int _transpositionTableCapacity;
        Position _position;
        public MoveComparer(Dictionary<int, TranspositionEntry> transpositionTable, int ttCapacity, Position position)
        {
            _transpositionTable = transpositionTable;
            _transpositionTableCapacity = ttCapacity;
            _position = position;
        }
        public int Compare(Move x, Move y)
        {
            if (x.Equals(y)) return 0;

            int hash = (int)(_position.Hash() % (uint)_transpositionTableCapacity);

            // Check hash table first
            if(_transpositionTable.ContainsKey(hash) && (int)(_transpositionTable[hash].HashKey % (uint)_transpositionTableCapacity) == hash)
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

            bool xIsCapture = x.IsCapture();
            bool yIsCapture = y.IsCapture();

            if(xIsCapture && !yIsCapture)
            {
                return -950;
            }
            if (yIsCapture && !xIsCapture)
            {
                return 950;
            }
            if (xIsCapture && yIsCapture)
            {
                int[] pieceCapturingValues = new int[] { 1, 3, 3, 5, 9, 20 };
                int xVictimMinusAttacker = pieceCapturingValues[(int)x.GetPieceCapturing(_position)] - pieceCapturingValues[(int)x.Piece];
                int yVictimMinusAttacker = pieceCapturingValues[(int)y.GetPieceCapturing(_position)] - pieceCapturingValues[(int)y.Piece];
                if (xVictimMinusAttacker > yVictimMinusAttacker) return -800 - xVictimMinusAttacker;
                if (yVictimMinusAttacker > xVictimMinusAttacker) return 800 + yVictimMinusAttacker;
            }
            return 0;
        }
    }
}
