using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MegaKnight.Core;
using System.Diagnostics;

namespace MegaKnight.Debugging
{
    /// <summary>
    /// Displays the number of legal moves at certain depths from a starting position.
    /// </summary>
    internal class Perft
    {
        MoveGenerator _moveGenerator;
        BotCore _core;
        public Perft(MoveGenerator moveGenerator, BotCore core)
        {
            _moveGenerator = moveGenerator;
            _core = core;
        }
        public void RunPerft(Position startPosition, int maxDepth)
        {
            Debug.WriteLine("Running Perft on depth " + maxDepth);
            List<Move> moves = _moveGenerator.GenerateAllPossibleMoves(startPosition);
            ulong totalNodes = 0ul;
            foreach(Move move in moves)
            {
                Debug.Write(move.ToString() + ": ");
                Position p = (Position)startPosition.Clone();
                _core.MakeMove(move, p);
                ulong subnodes = PerftRecursive(p, maxDepth - 1);
                Debug.WriteLine(subnodes);
                totalNodes += subnodes;
            }
            Debug.WriteLine("Number of nodes: " + totalNodes);
        }
        ulong PerftRecursive(Position position, int depth)
        {
            List<Move> moves = _moveGenerator.GenerateAllPossibleMoves(position);
            ulong nodes = 0ul;
            if (depth == 0)
            {
                return 1ul;
            }
            if(depth == 1)
            {
                return (ulong)moves.Count;
            }
            foreach(Move move in moves)
            {
                Position p = (Position)position.Clone();
                _core.MakeMove(move, p);
                nodes += PerftRecursive(p, depth - 1);
            }
            return nodes;
        }
    }
}
