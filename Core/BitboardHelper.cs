using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MegaKnight.Core
{
    internal static class BitboardHelper
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
        public static int[] BoardToArrayOfIndeces(ulong bitboard)
        {
            int[] indeces = new int[GetBitboardPopCount(bitboard)];
            int indexCount = 0;
            int i = 0;
            while(bitboard > 0)
            {
                if((bitboard & 1) > 0)
                {
                    indeces[i] = indexCount;
                    i++;
                    bitboard &= bitboard - 1;
                }
                int trailCount = BitOperations.TrailingZeroCount(bitboard);
                if (trailCount == 64) return indeces;
                bitboard >>= trailCount;
                indexCount += trailCount;
            }
            return indeces;
        }
        // Precondition: Bitboard represents a ulong with exactly one bit
        public static int SinglePopBitboardToIndex(ulong bitboard)
        {
            if (bitboard == 0)
                throw new Exception("Cannot get single index of empty bitboard");

            if (GetBitboardPopCount(bitboard) != 1)
            {
                PrintBitboard(bitboard);
                throw new Exception("Cannot get single index of bitboard with multiple 1's");
            }
            return BitOperations.TrailingZeroCount(bitboard);
        }
        public static int GetBitboardPopCount(ulong bitboard)
        {
            return BitOperations.PopCount(bitboard);
        }
    }
}
