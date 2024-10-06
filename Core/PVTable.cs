using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MegaKnight.Core
{
    /// <summary>
    /// Triangular PV table. Stores an array that stores the principal variation of different search depths sequentially, starting at depth 1.
    /// </summary>
    /* Table (each row is the principal variation at a certain depth.
     * M
     * M M
     * M M M
     * M M M M
     * M M M M ... M
     */
    internal class PVTable
    {
        Move[] _moves;
        int _maxDepth = 1;
        public PVTable()
        {
            _moves = new Move[1];
        }
        int GetStartIndex(int depth)
        {
            // Subtract 1 because, if we want the start index of depth 1, we look at index 0
            int d = depth - 1;
            return (d * d + d) / 2; // Triangular lookup
        }
        void EnsureDepth(int depth)
        {
            while(_maxDepth < depth)
            {
                Move[] moves = new Move[GetStartIndex(_maxDepth + 2) - 1];
                for(int i = 0; i < GetStartIndex(_maxDepth + 1) - 1; i++)
                {
                    moves[i] = _moves[i];
                }
                _moves = moves;
                _maxDepth++;
                Debug.WriteLine("Increased depth of PV table to " + _maxDepth);
            }
        }
        public Move[] GetPrincipalVariation()
        {
            return GetPrincipalVariation(_maxDepth);
        }

        Move[] GetPrincipalVariation(int depth)
        {
            Move[] m = new Move[depth];
            int i = GetStartIndex(depth);
            Array.Copy(_moves, i, m, 0, depth - 1);
            return m;
        }
        public void SetPVValue(Move move, int depth, int offset)
        {
            int i = GetStartIndex(depth) + offset;
            EnsureDepth(depth + 1);
            _moves[i] = move;
        }
        public override string ToString()
        {
            string str = "";
            int d = 1;
            for(int i = 0; i < GetStartIndex(_maxDepth); i++)
            {
                if (_moves[i] != null)
                {
                    str += _moves[i].ToString() + " ";
                }
                else
                {
                    str += "-    ";
                }
                if(i == GetStartIndex(d + 1) - 1)
                {
                    d++;
                    str += "\n";
                }
            }
            return str;
        }
    }
}
