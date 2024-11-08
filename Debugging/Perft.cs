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
        public void RunPerftConsole(Position startPosition, int maxDepth)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            List<Move> moves = _moveGenerator.GenerateAllPossibleMoves(startPosition);
            ulong totalNodes = 0ul;
            foreach (Move move in moves)
            {
                Console.Write(move.ToString() + ": ");
                startPosition.MakeMove(move);
                ulong subnodes = PerftRecursive(startPosition, maxDepth - 1);
                startPosition.UnmakeMove(move);
                Console.WriteLine(subnodes);
                totalNodes += subnodes;
            }
            Console.WriteLine("Number of nodes: " + totalNodes);
            stopwatch.Stop();
            Console.WriteLine("Total seconds: " + stopwatch.ElapsedMilliseconds / 1000f);
            Console.WriteLine("Nodes per second: " + totalNodes / (stopwatch.ElapsedMilliseconds / 1000f));
        }

        public void RunPerftDebug(Position startPosition, int maxDepth)
        {
            Debug.WriteLine("Running Perft on depth " + maxDepth);
            Stopwatch stopwatch = Stopwatch.StartNew();
            List<Move> moves = _moveGenerator.GenerateAllPossibleMoves(startPosition);
            ulong totalNodes = 0ul;
            foreach(Move move in moves)
            {
                Debug.Write(move.ToString() + ": ");
                startPosition.MakeMove(move);
                ulong subnodes = PerftRecursive(startPosition, maxDepth - 1);
                startPosition.UnmakeMove(move);
                Debug.WriteLine(subnodes);
                totalNodes += subnodes;
            }
            Debug.WriteLine("Number of nodes: " + totalNodes);
            stopwatch.Stop();
            Debug.WriteLine("Total seconds: " + stopwatch.ElapsedMilliseconds / 1000f);
            Debug.WriteLine("Nodes per second: " + totalNodes / (stopwatch.ElapsedMilliseconds / 1000f));
            Debug.WriteLine("");
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
                position.MakeMove(move);
                //Debug.WriteLine(move.ToString());
                nodes += PerftRecursive(position, depth - 1);
                position.UnmakeMove(move);
            }
            return nodes;
        }
    }
}
