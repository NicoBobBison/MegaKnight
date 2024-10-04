using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MegaKnight.Core
{
    internal static class EvalWeights
    {
        // CREDIT: Most evaluation weights are taken from PeSTO's evaluation function
        // https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function

        // All weights are in centipawns (1 centipawn = 0.01 pawns)
        // Weights for value of a piece
        public const int PawnValueEarly = 82;
        public const int KnightValueEarly = 337;
        public const int BishopValueEarly = 365;
        public const int RookValueEarly = 477;
        public const int QueenValueEarly = 1025;

        public const int PawnValueLate = 94;
        public const int KnightValueLate = 281;
        public const int BishopValueLate = 297;
        public const int RookValueLate = 512;
        public const int QueenValueLate = 936;

        // Weight for mobility of pieces
        public const int PawnMobilityValue = 1;
        public const int KnightMobilityValue = 3;
        public const int BishopMobilityValue = 3;
        public const int RookMobilityValue = 2;
        public const int QueenMobilityValue = 1;

        // Early game piece-square tables (from white's perspective, mirror for black's perspective)
        // TODO: Write and test early and late game (middlegame?) piece-square tables, research existing PST's
        public static readonly int[] PawnPSTEarly = new int[]
        {
            0,  0,  0,  0,  0,  0,  0,  0,
            35, 40, 40, 40, 40, 40, 40, 40,
            30, 35, 35, 35, 35, 35, 35, 30,
            25, 30, 30, 30, 30, 30, 30, 25,
            20, 25, 30, 35, 35, 30, 25, 20,
            15, 20, 25, 20, 20, 25, 20, 15,
            10, 10, 10, 10, 10, 10, 10, 10,
            0,  0,  0,  0,  0,  0,  0,  0,
        };
        public static void Initialize()
        {

        }
    }
}
