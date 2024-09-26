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
        public ulong GenerateMoves(ulong startSquare, Piece piece, Position position)
        {
            ulong piecesAttackingKing = GetPiecesAttackingKing(position);
            int numAttackers = BitboardHelper.GetBitboardPopCount(piecesAttackingKing);

            // We need to store move and capture masks separately because of en passant potentially blocking a check
            ulong moveMask = ulong.MaxValue;
            ulong captureMask = ulong.MaxValue;

            if(piece != Piece.King)
            {
                // If we are in double check, we can only move our king
                if (numAttackers >= 2)
                {
                    return 0ul;
                }
                else if (numAttackers == 1)
                {
                    // We can only capture the attacking piece
                    captureMask = piecesAttackingKing;
                    if (position.IsSlidingPiece(piecesAttackingKing))
                    {
                        moveMask = GetSquaresBetweenPiecesRay(position.WhiteToMove ? position.WhiteKing : position.BlackKing, piecesAttackingKing);
                    }
                    else
                    {
                        moveMask = 0ul;
                    }
                }
            }

            switch (piece)
            {
                case Piece.Pawn:
                    return GeneratePawnMoves(startSquare, position, moveMask, captureMask);
                case Piece.Knight:
                    return GenerateKnightMoves(startSquare, position.WhiteToMove, position, moveMask, captureMask);
                case Piece.Bishop:
                    return GenerateBishopMoves(startSquare, position.WhiteToMove, position, moveMask, captureMask);
                case Piece.Rook:
                    return GenerateRookMoves(startSquare, position.WhiteToMove, position, moveMask, captureMask);
                case Piece.Queen:
                    return GenerateQueenMoves(startSquare, position.WhiteToMove, position, moveMask, captureMask);
                case Piece.King:
                    return GenerateKingMoves(startSquare, position);
            }
            throw new NotImplementedException("Piece is not accounted for in GenerateMoves");
        }

        // Assumed that the color of the piece moving is based on who's turn it is in the position
        ulong GeneratePawnMoves(ulong pawnPosition, Position position, ulong moveMask = ulong.MaxValue, ulong captureMask = ulong.MaxValue)
        {
            ulong moves = 0ul;
            if (position.WhiteToMove)
            {
                ulong oneForward = (pawnPosition << 8) & ~position.AllPieces & moveMask;
                moves |= oneForward;
                if(BitboardHelper.SinglePopBitboardToIndex(pawnPosition) / 8 == 1 && oneForward > 0)
                {
                    moves |= (pawnPosition << 16) & ~position.AllPieces & moveMask;
                }
                // Each attacked square must either a) be in the capture mask or b) be in the move mask AND be an en passant capture
                moves |= GeneratePawnAttacks(pawnPosition, position.WhiteToMove, position) & ((captureMask & position.BlackPieces) | (moveMask & 1ul << position.BlackEnPassantIndex));
            }
            else
            {
                ulong oneForward = (pawnPosition >> 8) & ~position.AllPieces & moveMask;
                moves |= oneForward;
                if (BitboardHelper.SinglePopBitboardToIndex(pawnPosition) / 8 == 6 && oneForward > 0)
                {
                    moves |= (pawnPosition >> 16) & ~position.AllPieces & moveMask;
                }
                moves |= GeneratePawnAttacks(pawnPosition, position.WhiteToMove, position) & ((captureMask & position.WhitePieces) | (moveMask & 1ul << position.WhiteEnPassantIndex));
            }
            return moves;
        }
        ulong GeneratePawnAttacks(ulong pawnPosition, bool pawnIsWhite, Position position)
        {
            return _pawnAttacks[pawnIsWhite ? (int)PieceColor.White : (int)PieceColor.Black, BitboardHelper.SinglePopBitboardToIndex(pawnPosition)];
        }
        ulong GenerateKnightMoves(ulong knightPosition, bool knightIsWhite, Position position, ulong moveMask = ulong.MaxValue, ulong captureMask = ulong.MaxValue)
        {
            int index = BitboardHelper.SinglePopBitboardToIndex(knightPosition);
            ulong knightMoves = _knightAttacks[index] & (moveMask | captureMask);
            ulong friendlyBlockers = knightIsWhite ? position.WhitePieces : position.BlackPieces;
            return knightMoves & ~friendlyBlockers;
        }
        ulong GenerateRookMoves(ulong rookPosition, bool rookIsWhite, Position position, ulong moveMask = ulong.MaxValue, ulong captureMask = ulong.MaxValue)
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
            moves &= moveMask | captureMask;

            ulong friendlyPieces = rookIsWhite ? position.WhitePieces : position.BlackPieces;
            return moves & ~friendlyPieces;
        }
        ulong GenerateBishopMoves(ulong bishopPosition, bool bishopIsWhite, Position position, ulong moveMask = ulong.MaxValue, ulong captureMask = ulong.MaxValue)
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
            moves &= moveMask | captureMask;

            ulong friendlyPieces = bishopIsWhite ? position.WhitePieces : position.BlackPieces;
            return moves & ~friendlyPieces;
        }
        ulong GenerateQueenMoves(ulong queenPosition, bool queenIsWhite, Position position, ulong moveMask = ulong.MaxValue, ulong captureMask = ulong.MaxValue)
        {
            return GenerateBishopMoves(queenPosition, queenIsWhite, position, moveMask, captureMask) | GenerateRookMoves(queenPosition, queenIsWhite, position, moveMask, captureMask);
        }
        ulong GenerateKingMoves(ulong kingPosition, Position position)
        {
            return GenerateKingMovesNoChecks(kingPosition, position) & ~GetKingCheckSquares(position);
        }
        ulong GenerateKingMovesNoChecks(ulong kingPosition, Position position)
        {
            int index = BitboardHelper.SinglePopBitboardToIndex(kingPosition);
            ulong friendlyBlockers = position.WhiteToMove ? position.WhitePieces : position.BlackPieces;
            return _kingAttacks[index] & ~friendlyBlockers;
        }
        ulong GetPiecesAttackingKing(Position position)
        {
            ulong attackers = 0ul;
            if (position.WhiteToMove)
            {
                attackers |= GeneratePawnAttacks(position.WhiteKing, true, position) & position.BlackPawns;
                attackers |= GenerateKnightMoves(position.WhiteKing, true, position) & position.BlackKnights;
                attackers |= GenerateBishopMoves(position.WhiteKing, true, position) & position.BlackBishops;
                attackers |= GenerateRookMoves(position.WhiteKing, true, position) & position.BlackRooks;
                attackers |= GenerateQueenMoves(position.WhiteKing, true, position) & position.BlackQueens;
            }
            else
            {
                attackers |= GeneratePawnAttacks(position.BlackKing, false, position) & position.WhitePawns;
                attackers |= GenerateKnightMoves(position.BlackKing, false, position) & position.WhiteKnights;
                attackers |= GenerateBishopMoves(position.BlackKing, false, position) & position.WhiteBishops;
                attackers |= GenerateRookMoves(position.BlackKing, false, position) & position.WhiteRooks;
                attackers |= GenerateQueenMoves(position.BlackKing, false, position) & position.WhiteQueens;
            }
            return attackers;
        }
        /// <summary>
        /// Casts rays from piece 1 to search for piece 2. When found, returns the squares between the pieces along a line.
        /// </summary>
        /// <param name="piece1"></param>
        /// <param name="piece2"></param>
        /// <returns>A mask of squares between the pieces. Returns 0ul if there is no line between pieces.</returns>
        ulong GetSquaresBetweenPiecesRay(ulong piece1, ulong piece2)
        {
            int piece1Index = BitboardHelper.SinglePopBitboardToIndex(piece1);
            int piece2Index = BitboardHelper.SinglePopBitboardToIndex(piece2);
            for(int direction = 0; direction < 8; direction++)
            {
                ulong ray = _rayAttacks[piece1Index, direction];
                if((ray & piece2) > 0)
                {
                    return ray & ~_rayAttacks[piece2Index, direction] & ~piece2;
                }
            }
            return 0ul;
        }
        ulong GetKingCheckSquares(Position position)
        {
            // Need to clone this because we want to make the king disappear without changing the original object
            Position p = (Position)position.Clone();
            ulong attacks = 0ul;
            if (p.WhiteToMove)
            {
                // Does this make the king disappear?
                p.WhiteKing = 0ul;
                foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(p.BlackPawns))
                {
                    attacks |= GeneratePawnAttacks(1ul << i, false, p);
                }
                foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(p.BlackKnights))
                {
                    attacks |= GenerateKnightMoves(1ul << i, false, p);
                }
                foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(p.BlackBishops))
                {
                    attacks |= GenerateBishopMoves(1ul << i, false, p);
                }
                foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(p.BlackRooks))
                {
                    attacks |= GenerateRookMoves(1ul << i, false, p);
                }
                foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(p.BlackQueens))
                {
                    attacks |= GenerateQueenMoves(1ul << i, false, p);
                }
                foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(p.BlackKing))
                {
                    attacks |= GenerateKingMovesNoChecks(1ul << i, p);
                }
            }
            else
            {
                p.BlackKing = 0ul;
                foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(p.WhitePawns))
                {
                    attacks |= GeneratePawnAttacks(1ul << i, true, p);
                }
                foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(p.WhiteKnights))
                {
                    attacks |= GenerateKnightMoves(1ul << i, true, p);
                }
                foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(p.WhiteBishops))
                {
                    attacks |= GenerateBishopMoves(1ul << i, false, p);
                }
                foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(p.WhiteRooks))
                {
                    attacks |= GenerateRookMoves(1ul << i, true, p);
                }
                foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(p.WhiteQueens))
                {
                    attacks |= GenerateQueenMoves(1ul << i, true, p);
                }
                foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(p.WhiteKing))
                {
                    attacks |= GenerateKingMovesNoChecks(1ul << i, p);
                }

            }
            return attacks;
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
