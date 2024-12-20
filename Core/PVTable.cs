﻿using System;
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
                Move[] moves = new Move[GetStartIndex(_maxDepth + 2)];
                for(int i = 0; i < GetStartIndex(_maxDepth + 1); i++)
                {
                    moves[i] = _moves[i];
                }
                _moves = moves;
                _maxDepth++;
            }
        }
        public Move[] GetPrincipalVariation()
        {
            return GetPrincipalVariation(_maxDepth - 1);
        }

        Move[] GetPrincipalVariation(int depth)
        {
            Move[] m = new Move[depth];
            int i = GetStartIndex(depth);
            Array.Copy(_moves, i, m, 0, depth);
            return m;
        }
        void PropogatePVValueDown(int depth)
        {
            for(int i = 0; i < depth; i++)
            {
                if (_moves[GetStartIndex(depth) + i] == null) return;
                _moves[GetStartIndex(depth + 1) + 1 + i] = _moves[GetStartIndex(depth) + i];
            }
        }
        public void SetPVValue(Move move, int depth)
        {
            // Debug.WriteLine("Set PV: move = " + move.ToString() + ", depth = " + depth);
            int i = GetStartIndex(depth);
            EnsureDepth(depth + 1);
            _moves[i] = move;
            if(depth > 0)
            {
                PropogatePVValueDown(depth - 1);
            }
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
