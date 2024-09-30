using System;
using System.Collections.Generic;
using System.Numerics;

namespace MegaKnight.Core
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
        public List<Move> GenerateAllPossibleMoves(Position position)
        {
            List<Move> allMoves = new List<Move>();

            ulong pawns = position.WhiteToMove ? position.WhitePawns : position.BlackPawns;
            ulong knights = position.WhiteToMove ? position.WhiteKnights : position.BlackKnights;
            ulong bishops = position.WhiteToMove ? position.WhiteBishops : position.BlackBishops;
            ulong rooks = position.WhiteToMove ? position.WhiteRooks : position.BlackRooks;
            ulong queens = position.WhiteToMove ? position.WhiteQueens : position.BlackQueens;
            ulong king = position.WhiteToMove ? position.WhiteKing : position.BlackKing;
            ulong enemyPieces = position.WhiteToMove ? position.BlackPieces : position.WhitePieces;

            foreach(int i in BitboardHelper.BitboardToListOfSquareIndeces(pawns))
            {
                ulong moves = GenerateMoves(1ul << i, Piece.Pawn, position);
                foreach (int j in BitboardHelper.BitboardToListOfSquareIndeces(moves))
                {
                    bool isCapture = (1ul << j & enemyPieces) > 0;
                    bool isPromotion = j >= 56 || j < 8;
                    bool isEnPassant = j == position.EnPassantTargetSquare;
                    if (Math.Abs(i - j) == 16)
                    {
                        allMoves.Add(new Move(Piece.Pawn, (Square)i, (Square)j, MoveType.DoublePawnPush));
                    }
                    else if (isEnPassant)
                    {
                        allMoves.Add(new Move(Piece.Pawn, (Square)i, (Square)j, MoveType.EnPassant));
                    }
                    else
                    {
                        if(isCapture && isPromotion)
                        {
                            allMoves.Add(new Move(Piece.Pawn, (Square)i, (Square)j, MoveType.KnightPromoCapture));
                            allMoves.Add(new Move(Piece.Pawn, (Square)i, (Square)j, MoveType.BishopPromoCapture));
                            allMoves.Add(new Move(Piece.Pawn, (Square)i, (Square)j, MoveType.RookPromoCapture));
                            allMoves.Add(new Move(Piece.Pawn, (Square)i, (Square)j, MoveType.QueenPromoCapture));
                        }
                        else if (isPromotion)
                        {
                            allMoves.Add(new Move(Piece.Pawn, (Square)i, (Square)j, MoveType.KnightPromotion));
                            allMoves.Add(new Move(Piece.Pawn, (Square)i, (Square)j, MoveType.BishopPromotion));
                            allMoves.Add(new Move(Piece.Pawn, (Square)i, (Square)j, MoveType.RookPromotion));
                            allMoves.Add(new Move(Piece.Pawn, (Square)i, (Square)j, MoveType.QueenPromotion));
                        }
                        else if (isCapture)
                        {
                            allMoves.Add(new Move(Piece.Pawn, (Square)i, (Square)j, MoveType.Capture));
                        }
                        else
                        {
                            allMoves.Add(new Move(Piece.Pawn, (Square)i, (Square)j));
                        }
                    }
                }
            }
            // TODO: Add other pieces
            foreach(int i in BitboardHelper.BitboardToListOfSquareIndeces(knights))
            {
                ulong moves = GenerateMoves(1ul << i, Piece.Knight, position);
                foreach(int j in BitboardHelper.BitboardToListOfSquareIndeces(moves))
                {
                    bool isCapture = (1ul << j & enemyPieces) > 0;
                    if (isCapture)
                    {
                        allMoves.Add(new Move(Piece.Knight, (Square)i, (Square)j, MoveType.Capture));
                    }
                    else
                    {
                        allMoves.Add(new Move(Piece.Knight, (Square)i, (Square)j));
                    }
                }
            }
            foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(bishops))
            {
                ulong moves = GenerateMoves(1ul << i, Piece.Bishop, position);
                foreach (int j in BitboardHelper.BitboardToListOfSquareIndeces(moves))
                {
                    bool isCapture = (1ul << j & enemyPieces) > 0;
                    if (isCapture)
                    {
                        allMoves.Add(new Move(Piece.Bishop, (Square)i, (Square)j, MoveType.Capture));
                    }
                    else
                    {
                        allMoves.Add(new Move(Piece.Bishop, (Square)i, (Square)j));
                    }
                }
            }
            foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(rooks))
            {
                ulong moves = GenerateMoves(1ul << i, Piece.Rook, position);
                foreach (int j in BitboardHelper.BitboardToListOfSquareIndeces(moves))
                {
                    bool isCapture = (1ul << j & enemyPieces) > 0;
                    if (isCapture)
                    {
                        allMoves.Add(new Move(Piece.Rook, (Square)i, (Square)j, MoveType.Capture));
                    }
                    else
                    {
                        allMoves.Add(new Move(Piece.Rook, (Square)i, (Square)j));
                    }
                }
            }
            foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(queens))
            {
                ulong moves = GenerateMoves(1ul << i, Piece.Queen, position);
                foreach (int j in BitboardHelper.BitboardToListOfSquareIndeces(moves))
                {
                    bool isCapture = (1ul << j & enemyPieces) > 0;
                    if (isCapture)
                    {
                        allMoves.Add(new Move(Piece.Queen, (Square)i, (Square)j, MoveType.Capture));
                    }
                    else
                    {
                        allMoves.Add(new Move(Piece.Queen, (Square)i, (Square)j));
                    }
                }
            }
            foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(king))
            {
                ulong moves = GenerateMoves(1ul << i, Piece.King, position);
                foreach (int j in BitboardHelper.BitboardToListOfSquareIndeces(moves))
                {
                    bool isCapture = (1ul << j & enemyPieces) > 0;
                    // TODO: Add castling
                    if (isCapture)
                    {
                        allMoves.Add(new Move(Piece.King, (Square)i, (Square)j, MoveType.Capture));
                    }
                    else
                    {
                        allMoves.Add(new Move(Piece.King, (Square)i, (Square)j));
                    }
                }
            }
            return allMoves;
        }
        public ulong GenerateMoves(ulong startSquare, Piece piece, Position position)
        {
            ulong friendlyPieces = position.WhiteToMove ? position.WhitePieces : position.BlackPieces;
            ulong enemyPieces = position.WhiteToMove ? position.BlackPieces : position.WhitePieces;
            ulong friendlyKing = position.WhiteToMove ? position.WhiteKing : position.BlackKing;

            ulong piecesAttackingKing = GetPiecesAttackingKing(position);
            int numAttackers = BitboardHelper.GetBitboardPopCount(piecesAttackingKing);

            // We need to store move and capture masks separately because of en passant potentially blocking a check
            ulong moveMask = ulong.MaxValue;
            ulong captureMask = ulong.MaxValue;

            if (piece != Piece.King)
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
                ulong enemyBQ = position.WhiteToMove ? position.BlackBishops | position.BlackQueens : position.WhiteBishops | position.WhiteQueens;
                ulong enemyRQ = position.WhiteToMove ? position.BlackRooks | position.BlackQueens : position.WhiteRooks | position.WhiteQueens;
                ulong pinners = XRayBishopAttacks(friendlyKing, friendlyPieces, position) & enemyBQ;
                // Remove this piece from the board temporarily
                foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(pinners))
                {
                    ulong overlap = GenerateBishopAttacks(1ul << i, (friendlyPieces ^ startSquare) | enemyPieces) & GenerateBishopAttacks(friendlyKing, (friendlyPieces ^ startSquare) | enemyPieces);
                    if((overlap & startSquare) > 0)
                    {
                        moveMask &= overlap;
                        captureMask &= overlap | 1ul << i;
                    }
                }
                pinners = XRayRookAttacks(friendlyKing, friendlyPieces, position) & enemyRQ;
                foreach(int i in BitboardHelper.BitboardToListOfSquareIndeces(pinners))
                {
                    ulong overlap = GenerateRookAttacks(1ul << i, (friendlyPieces ^ startSquare) | enemyPieces) & GenerateRookAttacks(friendlyKing, (friendlyPieces ^ startSquare) | enemyPieces);
                    if ((overlap & startSquare) > 0)
                    {
                        moveMask &= overlap;
                        captureMask &= overlap | 1ul << i;
                    }
                }
            }

            switch (piece)
            {
                case Piece.Pawn:
                    return GeneratePawnMoves(startSquare, position, moveMask, captureMask);
                case Piece.Knight:
                    return GenerateKnightMoves(startSquare, friendlyPieces, enemyPieces, moveMask, captureMask);
                case Piece.Bishop:
                    return GenerateBishopMoves(startSquare, friendlyPieces, enemyPieces, moveMask, captureMask);
                case Piece.Rook:
                    return GenerateRookMoves(startSquare, friendlyPieces, enemyPieces, moveMask, captureMask);
                case Piece.Queen:
                    return GenerateQueenMoves(startSquare, friendlyPieces, enemyPieces, moveMask, captureMask);
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
                ulong enPassant = moveMask & 1ul << position.EnPassantTargetSquare;
                // We can't en passant if it would cause a discovered check
                if(enPassant > 0 && (GenerateRookAttacks(position.WhiteKing, position.AllPieces ^ pawnPosition ^ 1ul << position.EnPassantTargetSquare - 8) & (position.BlackRooks | position.BlackQueens)) > 0)
                {
                    enPassant = 0ul;
                }
                // Each attacked square must either a) be in the capture mask or b) be in the move mask AND be an en passant capture
                moves |= GeneratePawnAttacks(pawnPosition, position.WhiteToMove, position) & ((captureMask & position.BlackPieces) | enPassant);
            }
            else
            {
                ulong oneForward = (pawnPosition >> 8) & ~position.AllPieces & moveMask;
                moves |= oneForward;
                if (BitboardHelper.SinglePopBitboardToIndex(pawnPosition) / 8 == 6 && oneForward > 0)
                {
                    moves |= (pawnPosition >> 16) & ~position.AllPieces & moveMask;
                }
                ulong enPassant = moveMask & 1ul << position.EnPassantTargetSquare;
                if (enPassant > 0 && (GenerateRookAttacks(position.BlackKing, position.AllPieces ^ pawnPosition ^ 1ul << position.EnPassantTargetSquare + 8) & (position.WhiteRooks | position.WhiteQueens)) > 0)
                {
                    enPassant = 0ul;
                }
                moves |= GeneratePawnAttacks(pawnPosition, position.WhiteToMove, position) & ((captureMask & position.WhitePieces) | enPassant);
            }
            return moves;
        }
        ulong GeneratePawnAttacks(ulong pawnPosition, bool pawnIsWhite, Position position)
        {
            return _pawnAttacks[pawnIsWhite ? (int)PieceColor.White : (int)PieceColor.Black, BitboardHelper.SinglePopBitboardToIndex(pawnPosition)];
        }
        ulong GenerateKnightMoves(ulong knightPosition, ulong friendlyPieces, ulong enemyPieces, ulong moveMask = ulong.MaxValue, ulong captureMask = ulong.MaxValue)
        {
            int index = BitboardHelper.SinglePopBitboardToIndex(knightPosition);
            ulong knightMoves = _knightAttacks[index] & (moveMask | captureMask);
            return knightMoves & ~friendlyPieces;
        }
        ulong GenerateRookMoves(ulong rookPosition, ulong friendlyPieces, ulong enemyPieces, ulong moveMask = ulong.MaxValue, ulong captureMask = ulong.MaxValue)
        {
            ulong moves = GenerateRookAttacks(rookPosition, friendlyPieces | enemyPieces);
            moves &= moveMask | captureMask;
            return moves & ~friendlyPieces;
        }
        ulong GenerateRookAttacks(ulong rookPosition, ulong occupancy)
        {
            ulong moves = 0ul;
            int indexOfPosition = BitOperations.TrailingZeroCount(rookPosition);

            ulong attackRay = _rayAttacks[indexOfPosition, (int)Direction.North];
            moves |= attackRay;
            if ((attackRay & occupancy) > 0)
            {
                int firstMaskedBlocker = BitOperations.TrailingZeroCount(attackRay & occupancy);
                moves &= ~_rayAttacks[firstMaskedBlocker, (int)Direction.North];
            }

            attackRay = _rayAttacks[indexOfPosition, (int)Direction.East];
            moves |= attackRay;
            if ((attackRay & occupancy) > 0)
            {
                int firstMaskedBlocker = BitOperations.TrailingZeroCount(attackRay & occupancy);
                moves &= ~_rayAttacks[firstMaskedBlocker, (int)Direction.East];
            }

            attackRay = _rayAttacks[indexOfPosition, (int)Direction.South];
            moves |= attackRay;
            if ((attackRay & occupancy) > 0)
            {
                int firstMaskedBlocker = 63 - BitOperations.LeadingZeroCount(attackRay & occupancy);
                moves &= ~_rayAttacks[firstMaskedBlocker, (int)Direction.South];
            }

            attackRay = _rayAttacks[indexOfPosition, (int)Direction.West];
            moves |= attackRay;
            if ((attackRay & occupancy) > 0)
            {
                int firstMaskedBlocker = 63 - BitOperations.LeadingZeroCount(attackRay & occupancy);
                moves &= ~_rayAttacks[firstMaskedBlocker, (int)Direction.West];
            }
            return moves;
        }
        ulong GenerateBishopMoves(ulong bishopPosition, ulong friendlyPieces, ulong enemyPieces, ulong moveMask = ulong.MaxValue, ulong captureMask = ulong.MaxValue)
        {
            ulong moves = GenerateBishopAttacks(bishopPosition, friendlyPieces | enemyPieces);
            moves &= moveMask | captureMask;
            return moves & ~friendlyPieces;
        }
        ulong GenerateBishopAttacks(ulong bishopPosition, ulong occupancy)
        {
            ulong moves = 0ul;
            int indexOfPosition = BitOperations.TrailingZeroCount(bishopPosition);

            ulong attackRay = _rayAttacks[indexOfPosition, (int)Direction.NorthEast];
            moves |= attackRay;
            if ((attackRay & occupancy) > 0)
            {
                int firstMaskedBlocker = BitOperations.TrailingZeroCount(attackRay & occupancy);
                moves &= ~_rayAttacks[firstMaskedBlocker, (int)Direction.NorthEast];
            }

            attackRay = _rayAttacks[indexOfPosition, (int)Direction.NorthWest];
            moves |= attackRay;
            if ((attackRay & occupancy) > 0)
            {
                int firstMaskedBlocker = BitOperations.TrailingZeroCount(attackRay & occupancy);
                moves &= ~_rayAttacks[firstMaskedBlocker, (int)Direction.NorthWest];
            }

            attackRay = _rayAttacks[indexOfPosition, (int)Direction.SouthEast];
            moves |= attackRay;
            if ((attackRay & occupancy) > 0)
            {
                int firstMaskedBlocker = 63 - BitOperations.LeadingZeroCount(attackRay & occupancy);
                moves &= ~_rayAttacks[firstMaskedBlocker, (int)Direction.SouthEast];
            }

            attackRay = _rayAttacks[indexOfPosition, (int)Direction.SouthWest];
            moves |= attackRay;
            if ((attackRay & occupancy) > 0)
            {
                int firstMaskedBlocker = 63 - BitOperations.LeadingZeroCount(attackRay & occupancy);
                moves &= ~_rayAttacks[firstMaskedBlocker, (int)Direction.SouthWest];
            }
            return moves;
        }

        ulong GenerateQueenMoves(ulong queenPosition, ulong friendlyPieces, ulong enemyPieces, ulong moveMask = ulong.MaxValue, ulong captureMask = ulong.MaxValue)
        {
            return GenerateBishopMoves(queenPosition, friendlyPieces, enemyPieces, moveMask, captureMask) | GenerateRookMoves(queenPosition, friendlyPieces, enemyPieces, moveMask, captureMask);
        }
        ulong GenerateKingMoves(ulong kingPosition, Position position)
        {
            // TODO: Add castling
            ulong checkedSquares = GetKingCheckSquares(position);
            return (GenerateKingMovesRaw(kingPosition, position) | GenerateKingCastleMoves(checkedSquares, position)) & ~checkedSquares;
        }
        ulong GenerateKingMovesRaw(ulong kingPosition, Position position)
        {
            int index = BitboardHelper.SinglePopBitboardToIndex(kingPosition);
            ulong friendlyBlockers = position.WhiteToMove ? position.WhitePieces : position.BlackPieces;
            return _kingAttacks[index] & ~friendlyBlockers;
        }
        ulong GenerateKingCastleMoves(ulong checkedSquares, Position position)
        {
            ulong friendlyKing = position.WhiteToMove ? position.WhiteKing : position.BlackKing;
            if ((friendlyKing & checkedSquares) > 0)
            {
                // Can't castle in check
                return 0ul;
            }
            ulong castleMoves = 0ul;
            ulong whiteKingRookOrigin = 1ul << 7;
            ulong whiteQueenRookOrigin = 1ul;
            ulong blackKingRookOrigin = 1ul << 63;
            ulong blackQueenRookOrigin = 1ul << 56;
            ulong whiteKingOrigin = 1ul << 4;
            ulong blackKingOrigin = 1ul << 60;
            ulong piecesAndCheckedSquares = position.AllPieces | checkedSquares;
            // Check if we have castling rights and if rook is on correct square (to check if rook has been captured)
            bool kingSideCastle = position.WhiteToMove ? position.WhiteKingCastle && (position.WhiteRooks & whiteKingRookOrigin) > 0 :
                                                         position.BlackKingCastle && (position.BlackRooks & blackKingRookOrigin) > 0;
            bool queenSideCastle = position.WhiteToMove ? position.WhiteQueenCastle && (position.WhiteRooks & whiteQueenRookOrigin) > 0 :
                                                          position.BlackQueenCastle && (position.BlackRooks & blackQueenRookOrigin) > 0;
            if (position.WhiteToMove)
            {
                if(kingSideCastle && ((whiteKingOrigin << 1 | whiteKingOrigin << 2)  & piecesAndCheckedSquares) == 0)
                {
                    castleMoves |= whiteKingOrigin << 2;
                }
                if (queenSideCastle && ((whiteKingOrigin >> 1 | whiteKingOrigin >> 2) & piecesAndCheckedSquares) == 0 && (whiteKingOrigin >> 3 & position.AllPieces) == 0)
                {
                    castleMoves |= whiteKingOrigin >> 2;
                }
            }
            else
            {
                if (kingSideCastle && ((blackKingOrigin << 1 | blackKingOrigin << 2) & piecesAndCheckedSquares) == 0)
                {
                    castleMoves |= blackKingOrigin << 2;
                }
                if (queenSideCastle && ((blackKingOrigin >> 1 | blackKingOrigin >> 2) & piecesAndCheckedSquares) == 0 && (blackKingOrigin >> 3 & position.AllPieces) == 0)
                {
                    castleMoves |= blackKingOrigin >> 2;
                }
            }
            return castleMoves;
        }
        /// <summary>
        /// Calculates a bishop attack while going through the first blocker hit. Useful for pins.
        /// </summary>
        /// <param name="bishopPosition"></param>
        /// <param name="blockers">Pieces that can block the bishop ray</param>
        /// <param name="position"></param>
        /// <returns></returns>
        ulong XRayBishopAttacks(ulong bishopPosition, ulong blockers, Position position)
        {
            ulong bishopAttacks = GenerateBishopAttacks(bishopPosition, position.AllPieces);
            blockers &= bishopAttacks;
            return bishopAttacks ^ GenerateBishopAttacks(bishopPosition, position.AllPieces ^ blockers);
        }
        /// <summary>
        /// Calculates a rook attack while going through the first blocker hit. Useful for pins.
        /// </summary>
        /// <param name="rookPosition"></param>
        /// <param name="blockers">Pieces that can block the rook ray</param>
        /// <param name="position"></param>
        /// <returns></returns>
        ulong XRayRookAttacks(ulong rookPosition, ulong blockers, Position position)
        {
            ulong rookAttacks = GenerateRookAttacks(rookPosition, position.AllPieces);
            blockers &= rookAttacks;
            return rookAttacks ^ GenerateRookAttacks(rookPosition, position.AllPieces ^ blockers);
        }
        public ulong GetPiecesAttackingKing(Position position)
        {
            ulong attackers = 0ul;
            if (position.WhiteToMove)
            {
                attackers |= GeneratePawnAttacks(position.WhiteKing, true, position) & position.BlackPawns;
                attackers |= GenerateKnightMoves(position.WhiteKing, position.WhitePieces, position.BlackPieces) & position.BlackKnights;
                attackers |= GenerateBishopMoves(position.WhiteKing, position.WhitePieces, position.BlackPieces) & position.BlackBishops;
                attackers |= GenerateRookMoves(position.WhiteKing, position.WhitePieces, position.BlackPieces) & position.BlackRooks;
                attackers |= GenerateQueenMoves(position.WhiteKing, position.WhitePieces, position.BlackPieces) & position.BlackQueens;
            }
            else
            {
                attackers |= GeneratePawnAttacks(position.BlackKing, false, position) & position.WhitePawns;
                attackers |= GenerateKnightMoves(position.BlackKing, position.BlackPieces, position.WhitePieces) & position.WhiteKnights;
                attackers |= GenerateBishopMoves(position.BlackKing, position.BlackPieces, position.WhitePieces) & position.WhiteBishops;
                attackers |= GenerateRookMoves(position.BlackKing, position.BlackPieces, position.WhitePieces) & position.WhiteRooks;
                attackers |= GenerateQueenMoves(position.BlackKing, position.BlackPieces, position.WhitePieces) & position.WhiteQueens;
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
                p.WhiteKing = 0ul;
                foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(p.BlackPawns))
                {
                    attacks |= GeneratePawnAttacks(1ul << i, false, p);
                }
                foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(p.BlackKnights))
                {
                    attacks |= GenerateKnightMoves(1ul << i, p.WhitePieces, p.BlackPieces);
                }
                foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(p.BlackBishops))
                {
                    attacks |= GenerateBishopMoves(1ul << i, p.WhitePieces, p.BlackPieces);
                }
                foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(p.BlackRooks))
                {
                    attacks |= GenerateRookMoves(1ul << i, p.WhitePieces, p.BlackPieces);
                }
                foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(p.BlackQueens))
                {
                    attacks |= GenerateQueenMoves(1ul << i, p.WhitePieces, p.BlackPieces);
                }
                foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(p.BlackKing))
                {
                    attacks |= GenerateKingMovesRaw(1ul << i, p);
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
                    attacks |= GenerateKnightMoves(1ul << i, p.BlackPieces, p.WhitePieces);
                }
                foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(p.WhiteBishops))
                {
                    attacks |= GenerateBishopMoves(1ul << i, p.BlackPieces, p.WhitePieces);
                }
                foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(p.WhiteRooks))
                {
                    attacks |= GenerateRookMoves(1ul << i, p.BlackPieces, p.WhitePieces);
                }
                foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(p.WhiteQueens))
                {
                    attacks |= GenerateQueenMoves(1ul << i, p.BlackPieces, p.WhitePieces);
                }
                foreach (int i in BitboardHelper.BitboardToListOfSquareIndeces(p.WhiteKing))
                {
                    attacks |= GenerateKingMovesRaw(1ul << i, p);
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
        ulong[] PrecomputeKingMoves()
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
