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
        enum PieceColor
        {
            White,
            Black
        }
        #region Precomputed board masks
        ulong[,] _rayAttacks;
        ulong[] _knightAttacks;
        ulong[,] _pawnAttacks;
        ulong[] _kingAttacks;
        #endregion

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
            _rayAttacks = PrecomputeAttackRays();
            _knightAttacks = PrecomputeKnightMoves();
            _pawnAttacks = PrecomputePawnAttacks();
            _kingAttacks = PrecomputeKingMoves();
        }
        public ulong GenerateMoves(Move move, Position position)
        {
            switch (move.Piece)
            {
                case Piece.Pawn:
                    return GeneratePawnMoves(move.StartSquare, position);
                case Piece.Knight:
                    return GenerateKnightMoves(move.StartSquare, position);
                case Piece.Bishop:
                    return GenerateBishopMoves(move.StartSquare, position);
                case Piece.Rook:
                    return GenerateRookMoves(move.StartSquare, position);
                case Piece.Queen:
                    return GenerateQueenMoves(move.StartSquare, position);
                case Piece.King:
                    return GenerateKingMoves(move.StartSquare, position);
            }
            throw new NotImplementedException("Need to add more pieces to move generator");
        }
        public ulong GenerateMoves(ulong startSquare, Piece piece, Position position)
        {
            switch (piece)
            {
                case Piece.Pawn:
                    return GeneratePawnMoves(startSquare, position);
                case Piece.Knight:
                    return GenerateKnightMoves(startSquare, position);
                case Piece.Bishop:
                    return GenerateBishopMoves(startSquare, position);
                case Piece.Rook:
                    return GenerateRookMoves(startSquare, position);
                case Piece.Queen:
                    return GenerateQueenMoves(startSquare, position);
                case Piece.King:
                    return GenerateKingMoves(startSquare, position);
            }
            throw new NotImplementedException("Need to add more pieces to move generator");
        }

        // Assumed that the color of the piece moving is based on who's turn it is in the position
        public ulong GeneratePawnMoves(ulong pawnPosition, Position position)
        {
            ulong moves = 0ul;
            if (position.WhiteToMove)
            {
                ulong oneForward = (pawnPosition << 8) & ~position.AllPieces;
                moves |= oneForward;
                if(BoardHelper.BitboardToIndex(pawnPosition) / 8 == 1 && oneForward > 0)
                {
                    moves |= (pawnPosition << 16) & ~position.AllPieces;
                }
                moves |= _pawnAttacks[(int)PieceColor.White, BoardHelper.BitboardToIndex(pawnPosition)] & (position.BlackPieces | 1ul << position.BlackEnPassantIndex);
            }
            else
            {
                ulong oneForward = (pawnPosition >> 8) & ~position.AllPieces;
                moves |= oneForward;
                if (BoardHelper.BitboardToIndex(pawnPosition) / 8 == 6 && oneForward > 0)
                {
                    moves |= (pawnPosition >> 16) & ~position.AllPieces;
                }
                moves |= _pawnAttacks[(int)PieceColor.Black, BoardHelper.BitboardToIndex(pawnPosition)] & (position.WhitePieces | 1ul << position.WhiteEnPassantIndex);
            }
            return moves;
        }
        public ulong GenerateKnightMoves(ulong knightPosition, Position position)
        {
            int index = BoardHelper.BitboardToIndex(knightPosition);
            ulong friendlyBlockers = position.WhiteToMove ? position.WhitePieces : position.BlackPieces;
            return _knightAttacks[index] & ~friendlyBlockers;
        }
        public ulong GenerateKingMoves(ulong kingPosition, Position position)
        {
            int index = BoardHelper.BitboardToIndex(kingPosition);
            ulong friendlyBlockers = position.WhiteToMove ? position.WhitePieces : position.BlackPieces;
            return _kingAttacks[index] & ~friendlyBlockers;
        }
        public ulong GenerateRookMoves(ulong rookPosition, Position position)
        {
            ulong moves = 0ul;
            int indexOfPosition = BitOperations.TrailingZeroCount(rookPosition);

            ulong attackRay = _rayAttacks[indexOfPosition, (int)Direction.North];
            moves |= attackRay;
            if((attackRay & position.AllPieces) > 0)
            {
                int firstMaskedBlocker = BitOperations.TrailingZeroCount(attackRay & position.AllPieces);
                moves &= ~_rayAttacks[firstMaskedBlocker, (int)Direction.North];
            }

            attackRay = _rayAttacks[indexOfPosition, (int)Direction.East];
            moves |= attackRay;
            if ((attackRay & position.AllPieces) > 0)
            {
                int firstMaskedBlocker = BitOperations.TrailingZeroCount(attackRay & position.AllPieces);
                moves &= ~_rayAttacks[firstMaskedBlocker, (int)Direction.East];
            }

            attackRay = _rayAttacks[indexOfPosition, (int)Direction.South];
            moves |= attackRay;
            if ((attackRay & position.AllPieces) > 0)
            {
                int firstMaskedBlocker = 63 - BitOperations.LeadingZeroCount(attackRay & position.AllPieces);
                moves &= ~_rayAttacks[firstMaskedBlocker, (int)Direction.South];
            }

            attackRay = _rayAttacks[indexOfPosition, (int)Direction.West];
            moves |= attackRay;
            if ((attackRay & position.AllPieces) > 0)
            {
                int firstMaskedBlocker = 63 - BitOperations.LeadingZeroCount(attackRay & position.AllPieces);
                moves &= ~_rayAttacks[firstMaskedBlocker, (int)Direction.West];
            }

            ulong friendlyPieces = position.WhiteToMove ? position.WhitePieces : position.BlackPieces;
            return moves & ~friendlyPieces;
        }
        public ulong GenerateBishopMoves(ulong bishopPosition, Position position)
        {
            ulong moves = 0ul;
            int indexOfPosition = BitOperations.TrailingZeroCount(bishopPosition);

            ulong attackRay = _rayAttacks[indexOfPosition, (int)Direction.NorthEast];
            moves |= attackRay;
            if ((attackRay & position.AllPieces) > 0)
            {
                int firstMaskedBlocker = BitOperations.TrailingZeroCount(attackRay & position.AllPieces);
                moves &= ~_rayAttacks[firstMaskedBlocker, (int)Direction.NorthEast];
            }

            attackRay = _rayAttacks[indexOfPosition, (int)Direction.NorthWest];
            moves |= attackRay;
            if ((attackRay & position.AllPieces) > 0)
            {
                int firstMaskedBlocker = BitOperations.TrailingZeroCount(attackRay & position.AllPieces);
                moves &= ~_rayAttacks[firstMaskedBlocker, (int)Direction.NorthWest];
            }

            attackRay = _rayAttacks[indexOfPosition, (int)Direction.SouthEast];
            moves |= attackRay;
            if ((attackRay & position.AllPieces) > 0)
            {
                int firstMaskedBlocker = 63 - BitOperations.LeadingZeroCount(attackRay & position.AllPieces);
                moves &= ~_rayAttacks[firstMaskedBlocker, (int)Direction.SouthEast];
            }

            attackRay = _rayAttacks[indexOfPosition, (int)Direction.SouthWest];
            moves |= attackRay;
            if ((attackRay & position.AllPieces) > 0)
            {
                int firstMaskedBlocker = 63 - BitOperations.LeadingZeroCount(attackRay & position.AllPieces);
                moves &= ~_rayAttacks[firstMaskedBlocker, (int)Direction.SouthWest];
            }

            ulong friendlyPieces = position.WhiteToMove ? position.WhitePieces : position.BlackPieces;
            return moves & ~friendlyPieces;
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
        ulong[] PrecomputeKnightMoves()
        {
            ulong[] moves = new ulong[64];
            for(int r = 0; r < 8; r++)
            {
                for(int c = 0; c< 8; c++)
                {
                    ulong position = 1ul << (r * 8 + c);
                    ulong board = 0ul;

                    // We don't need to check if the bitshift results in a ulong > 0 because then the bitwise-or would not affect the board (could probably change this with ray attacks)
                    if (c != 7)
                    {
                        board |= position << 17;
                        board |= position >> 15;
                        if (c != 6)
                        {
                            board |= position << 10;
                            board |= position >> 6;
                        }
                    }
                    if(c != 0)
                    {
                        board |= position << 15;
                        board |= position >> 17;
                        if (c != 1)
                        {
                            board |= position << 6;
                            board |= position >> 10;
                        }
                    }

                    moves[r * 8 + c] = board;
                }
            }
            return moves;
        }
        ulong[,] PrecomputePawnAttacks()
        {
            ulong[,] attacks = new ulong[2, 64];
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    ulong position = 1ul << (r * 8 + c);
                    ulong whiteBoard = 0ul;
                    ulong blackBoard = 0ul;

                    if(c != 0)
                    {
                        whiteBoard |= position << 7;
                        blackBoard |= position >> 9;
                    }
                    if(c != 7)
                    {
                        whiteBoard |= position << 9;
                        blackBoard |= position >> 7;
                    }

                    attacks[(int)PieceColor.White, r * 8 + c] = whiteBoard;
                    attacks[(int)PieceColor.Black, r * 8 + c] = blackBoard;
                }
            }
            return attacks;
        }
        public ulong[] PrecomputeKingMoves()
        {
            ulong[] moves = new ulong[64];
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    ulong position = 1ul << (r * 8 + c);
                    ulong board = 0ul;

                    // We don't need to check if the bitshift results in a ulong > 0 because then the bitwise-or would not affect the board (could probably change this with ray attacks)
                    board |= position << 8;
                    board |= position >> 8;
                    if (c != 7)
                    {
                        board |= position << 9;
                        board |= position << 1;
                        board |= position >> 7;
                    }
                    if (c != 0)
                    {
                        board |= position << 7;
                        board |= position >> 1;
                        board |= position >> 9;
                    }
                    moves[r * 8 + c] = board;
                }
            }
            return moves;
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
