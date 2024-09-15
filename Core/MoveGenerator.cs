using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ChessBot.Core
{
    internal class MoveGenerator
    {
        ulong[,] _rays;
        readonly int[] _directionBitShifts = new int[] { 8, 9, 1, -7, -8, -9, -1, 7 };
        enum Direction
        {
            North      = 0,
            NorthEast  = 1,
            East       = 2,
            SouthEast  = 3,
            South      = 4,
            SouthWest  = 5,
            West       = 6,
            NorthWest  = 7
        }
        public MoveGenerator()
        {
            _rays = PrecomputeAttackRays();
        }
        public ulong GenerateMoves(Move move, Position position)
        {
            switch (move.Piece)
            {
                case Piece.Bishop:
                    return GenerateBishopMoves(move.StartSquare, position);
                case Piece.Rook:
                    return GenerateRookMoves(move.StartSquare, position);
                case Piece.Queen:
                    return GenerateQueenMoves(move.StartSquare, position);
            }
            throw new NotImplementedException("Need to add more pieces to move generator");
        }
        // Assumed that the color of the rook is based on who's turn it is in the position
        public ulong GenerateRookMoves(ulong rookPosition, Position position)
        {
            ulong possibleMoves = 0ul;
            int indexOfPosition = BitOperations.TrailingZeroCount(rookPosition);

            ulong attackRay = _rays[indexOfPosition, (int)Direction.North];
            possibleMoves |= attackRay;
            if((attackRay & position.AllPieces) > 0)
            {
                int firstMaskedBlocker = BitOperations.TrailingZeroCount(attackRay & position.AllPieces);
                possibleMoves &= ~_rays[firstMaskedBlocker, (int)Direction.North];
            }

            attackRay = _rays[indexOfPosition, (int)Direction.East];
            possibleMoves |= attackRay;
            if ((attackRay & position.AllPieces) > 0)
            {
                int firstMaskedBlocker = BitOperations.TrailingZeroCount(attackRay & position.AllPieces);
                possibleMoves &= ~_rays[firstMaskedBlocker, (int)Direction.East];
            }

            attackRay = _rays[indexOfPosition, (int)Direction.South];
            possibleMoves |= attackRay;
            if ((attackRay & position.AllPieces) > 0)
            {
                int firstMaskedBlocker = 63 - BitOperations.LeadingZeroCount(attackRay & position.AllPieces);
                possibleMoves &= ~_rays[firstMaskedBlocker, (int)Direction.South];
            }

            attackRay = _rays[indexOfPosition, (int)Direction.West];
            possibleMoves |= attackRay;
            if ((attackRay & position.AllPieces) > 0)
            {
                int firstMaskedBlocker = 63 - BitOperations.LeadingZeroCount(attackRay & position.AllPieces);
                possibleMoves &= ~_rays[firstMaskedBlocker, (int)Direction.West];
            }

            ulong friendlyPieces = position.WhiteToMove ? position.WhitePieces : position.BlackPieces;
            return possibleMoves & ~friendlyPieces;
        }
        public ulong GenerateBishopMoves(ulong bishopPosition, Position position)
        {
            ulong possibleMoves = 0ul;
            int indexOfPosition = BitOperations.TrailingZeroCount(bishopPosition);

            ulong attackRay = _rays[indexOfPosition, (int)Direction.NorthEast];
            possibleMoves |= attackRay;
            if ((attackRay & position.AllPieces) > 0)
            {
                int firstMaskedBlocker = BitOperations.TrailingZeroCount(attackRay & position.AllPieces);
                possibleMoves &= ~_rays[firstMaskedBlocker, (int)Direction.NorthEast];
            }

            attackRay = _rays[indexOfPosition, (int)Direction.NorthWest];
            possibleMoves |= attackRay;
            if ((attackRay & position.AllPieces) > 0)
            {
                int firstMaskedBlocker = BitOperations.TrailingZeroCount(attackRay & position.AllPieces);
                possibleMoves &= ~_rays[firstMaskedBlocker, (int)Direction.NorthWest];
            }

            attackRay = _rays[indexOfPosition, (int)Direction.SouthEast];
            possibleMoves |= attackRay;
            if ((attackRay & position.AllPieces) > 0)
            {
                int firstMaskedBlocker = 63 - BitOperations.LeadingZeroCount(attackRay & position.AllPieces);
                possibleMoves &= ~_rays[firstMaskedBlocker, (int)Direction.SouthEast];
            }

            attackRay = _rays[indexOfPosition, (int)Direction.SouthWest];
            possibleMoves |= attackRay;
            if ((attackRay & position.AllPieces) > 0)
            {
                int firstMaskedBlocker = 63 - BitOperations.LeadingZeroCount(attackRay & position.AllPieces);
                possibleMoves &= ~_rays[firstMaskedBlocker, (int)Direction.SouthWest];
            }

            ulong friendlyPieces = position.WhiteToMove ? position.WhitePieces : position.BlackPieces;
            return possibleMoves & ~friendlyPieces;
        }
        public ulong GenerateQueenMoves(ulong queenPosition, Position position)
        {
            return GenerateBishopMoves(queenPosition, position) | GenerateRookMoves(queenPosition, position);
        }
        ulong[,] PrecomputeAttackRays()
        {
            ulong[,] rays = new ulong[64,8];

            for(int r = 0; r < 8; r++)
            {
                for(int c = 0; c < 8; c++)
                {
                    for(int direction = 0; direction < 8; direction++)
                    {
                        ulong positionCount = 1ul << (r * 8 + c);
                        ulong tempBoard = 0ul; // Use a temporary board because we don't want to include "not moving" as a valid move. Also easier for capture detection this way.
                        int rowCount = r;
                        int colCount = c;
                        Vector2 dirVector = DirectionToVector(_directionBitShifts[direction]);
                        while (positionCount > 0)
                        {
                            positionCount = _directionBitShifts[direction] > 0 ? positionCount << _directionBitShifts[direction] : positionCount >> -_directionBitShifts[direction];
                            rowCount += (int)dirVector.Y;
                            colCount += (int)dirVector.X;
                            if (rowCount < 0 || rowCount > 7 || colCount < 0 || colCount > 7) break;
                            tempBoard |= positionCount;
                        }
                        rays[r * 8 + c, direction] = tempBoard;
                    }
                }
            }
            return rays;
        }
        /// <summary>
        /// Converts a bit shift operation to a vector representing the shift direction relative to the starting square
        /// </summary>
        /// <param name="direction">Bit shift direction</param>
        /// <returns>Vector2 version of direction</returns>
        Vector2 DirectionToVector(int direction)
        {
            Vector2 dir = new Vector2();
            // 8, 9, 1, -7, -8, -9, -1, 7
            switch (direction)
            {
                case 8:
                    dir.Y = 1;
                    break;
                case 9:
                    dir.X = 1;
                    dir.Y = 1;
                    break;
                case 1:
                    dir.X = 1;
                    break;
                case -7:
                    dir.X = 1;
                    dir.Y = -1;
                    break;
                case -8:
                    dir.Y = -1;
                    break;
                case -9:
                    dir.X = -1;
                    dir.Y = -1;
                    break;
                case -1:
                    dir.X = -1;
                    break;
                case 7:
                    dir.X = -1;
                    dir.Y = 1;
                    break;
                default:
                    throw new Exception("Invalid direction");
            }
            return dir;
        }
    }
}
