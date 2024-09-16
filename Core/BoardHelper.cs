using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ChessBot.Core
{
    internal static class BoardHelper
    {
        public static void PrintBitboard(ulong board)
        {
            string str = "";
            for (int r = 0; r < 8; r++)
            {
                string rowStr = "";
                for (int c = 0; c < 8; c++)
                {
                    rowStr += ((board & 1) > 0 ? "1 " : "0 ");
                    board >>= 1;
                }
                str = "\n" + rowStr + str;
            }
            Debug.WriteLine(str);
        }
        public static void PrintSquareAsBitboard(int square)
        {
            if (square < 0 || square > 63) Debug.WriteLine("Cannot print " + square + ": out of bounds");
            PrintBitboard(1ul << square);
        }
        public static List<int> BitboardToListOfSquareIndeces(ulong bitboard)
        {
            List<int> indeces = new List<int>();
            int indexCount = 0;
            while(bitboard > 0)
            {
                if((bitboard & 1) > 0)
                {
                    indeces.Add(indexCount);
                }
                bitboard >>= 1;
                indexCount++;
            }
            return indeces;
        }
        // Precondition: Bitboard represents a ulong with exactly one bit
        public static int BitboardToIndex(ulong bitboard)
        {
            return BitOperations.TrailingZeroCount(bitboard);
        }
    }
}
