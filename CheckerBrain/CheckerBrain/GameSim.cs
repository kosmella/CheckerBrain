using System;
using System.Collections.Generic;
using System.Linq;

namespace Checkers
{
    //
    public enum Winner { noWinner, black, red };
    public static class GameSim
    {

        //has brain evaluate each potential move and returns a Board representing the move it chose.
        //Every CheckerBrain assumes it is black to cut down on the complexity of the neural network.
        //If the CheckerBrain in question is red, the board is inverted
        public static Board GetNextMove(Board currentState, CheckerBrain brain, bool isRed)
        {

            if (isRed)
                currentState.Invert();
            List<Board> potentialMoves = GetPossibleMoves(currentState);
            
            //no valid moves for this player
            if(potentialMoves.Count == 0)
            {
                if (isRed)
                    currentState.Invert();
                return currentState;
            }
            Board nextMove = brain.DecideNextMove(potentialMoves);
            if (isRed)
                nextMove.Invert();

            return nextMove;
        }

        //Gets all possible moves for the black player.
        public static List<Board> GetPossibleMoves(Board currentState)
        {
            //if a capture is possible, a player MUST take it.
            List<Board> moves = new List<Board>(); 
            for(int i = 0; i < 8; i++)
            {
                for(int j = 0; j < 8; j++)
                {
                    sbyte currentPiece = currentState.GetPieceAt(i, j);
                    if (currentPiece == Board.Black || currentPiece == Board.BlackKing)
                    {
                        moves.AddRange(GetCapturesForPiece(currentState, i, j));
                    }
                }
            }
            if(moves.Count()>0) //captures possible, so return ONLY those
                return moves;

            //No captures are possible so start looking at moves
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    sbyte currentPiece = currentState.GetPieceAt(i, j);
                    if (currentPiece == Board.Black || currentPiece == Board.BlackKing)
                    {
                        moves.AddRange(GetMovesForPiece(currentState, i, j));
                    }
                }
            }
            return moves;
        }

        //Determines if piece at (xFrom, yFrom) can make a capture by jumping to (xTo, yTo)
        public static bool CaptureIsPossible(Board state, int xFrom, int yFrom, int xTo, int yTo)
        {
            if(xFrom < 0 || xFrom >= 8 || yFrom < 0 || yFrom >= 8)
            {
                throw new InvalidOperationException("attempted to move piece out of bounds");
            }
            if(xTo < 0 || xTo >= 8 || yTo < 0 || yTo >= 8)//proposed move is out of bounds
                return false;
            if(state.GetPieceAt(xTo, yTo) != Board.Empty)//destination is not empty, capture cannot be made
                return false;
            if(yTo < yFrom  && state.GetPieceAt(xFrom, yFrom) != Board.BlackKing)//non-king can't move backwards
                return false;
            if (state.GetPieceAt((xFrom + xTo) / 2, (yFrom + yTo) / 2) != Board.Red &&
                state.GetPieceAt((xFrom + xTo) / 2, (yFrom + yTo) / 2) != Board.RedKing)//space being jumped does not contain enemy piece
                return false;
            return true;
        }

        //Moves piece at (xFrom, yFrom) to (xTo, yTo) and returns a board representing the new state
        public static Board ExecuteMove(Board state, int xFrom, int yFrom, int xTo, int yTo)
        {
            if (xTo < 0 || xTo >= 8 || yTo < 0 || yTo >= 8 || xFrom < 0 || xFrom >= 8 || yTo < 0 || yTo >= 8)
                throw new InvalidOperationException("ExecuteMove method called with out-of-bounds coordinates");

            sbyte[,] layout = state.CloneBoardState();
            layout[xTo, yTo] = layout[xFrom, yFrom];                    //Move piece to new space
            layout[xFrom, yFrom] = Board.Empty;                         //Origin space is empty
            if(Math.Abs(xFrom - xTo) == 2 || Math.Abs(yFrom - yTo) == 2)//piece was captured, make this space empty too
            {
                layout[(xFrom + xTo) / 2, (yFrom + yTo) / 2] = Board.Empty;
            }
            Board result = new Board(layout);
            return result;
        }

        //Recursive method that returns a list of boards representing all the captures that can be made by piece at (xFrom, yFrom)
        //Check if jump is possible in up/right direction
        //If jump possible, create a new board to represent the state after that jump was made, call that statePrime
        //Call GetCapturesForPiece on statePrime (checking for multi-captures)
        //If that list has size >0, add it to capturesFound
        //Else add boardPrime to capturesFound (if a multi-capture is possible it MUST be made, so only add boardPrime if no further jumps possible)
        //repeat for other 3 directions
        //return capturesFound
        public static List<Board> GetCapturesForPiece(Board state, int xFrom, int yFrom)
        {
            var capturesFound = new List<Board>();
            var multiCapturesFound = new List<Board>();
            Board statePrime;
            if (CaptureIsPossible(state, xFrom, yFrom, xFrom + 2, yFrom + 2))
            {
                //capture is possibe in up/right direction.
                statePrime = ExecuteMove(state, xFrom, yFrom, xFrom+2, yFrom+2);
                multiCapturesFound = GetCapturesForPiece(statePrime, xFrom + 2, yFrom + 2);
                if (multiCapturesFound.Count() > 0)
                {
                    capturesFound.AddRange(multiCapturesFound);
                }
                else
                {
                    if (yFrom + 2 == 7)//piece lands on the last row and becomes a king
                    {
                        statePrime.MakeKing(xFrom + 2, yFrom + 2);
                    }
                    capturesFound.Add(statePrime);
                }
            }
            if (CaptureIsPossible(state, xFrom, yFrom, xFrom - 2, yFrom + 2))
            {
                //capture is possibe in up/left direction.
               
                statePrime = ExecuteMove(state, xFrom, yFrom, xFrom -2, yFrom +2) ;
                multiCapturesFound = GetCapturesForPiece(statePrime, xFrom - 2, yFrom + 2);
                if (multiCapturesFound.Count() > 0)
                {
                    capturesFound.AddRange(multiCapturesFound);
                }
                else
                {
                    if (yFrom + 2 == 7)//piece lands on the last row and becomes a king
                    {
                        statePrime.MakeKing(xFrom - 2, yFrom + 2);
                    }
                    capturesFound.Add(statePrime);
                }
            }
            if (CaptureIsPossible(state, xFrom, yFrom, xFrom - 2, yFrom - 2))
            {
                //capture is possibe in down/left direction.
                statePrime = ExecuteMove(state, xFrom, yFrom, xFrom - 2, yFrom - 2);
                multiCapturesFound = GetCapturesForPiece(statePrime, xFrom - 2, yFrom - 2);
                if (multiCapturesFound.Count() > 0)
                {
                    capturesFound.AddRange(multiCapturesFound);
                }
                else
                {
                    capturesFound.Add(statePrime);
                }
            }
            if (CaptureIsPossible(state, xFrom, yFrom, xFrom + 2, yFrom - 2))
            {
                //capture is possibe in down/right direction.
                statePrime = ExecuteMove(state, xFrom, yFrom, xFrom +2, yFrom - 2);
                multiCapturesFound = GetCapturesForPiece(statePrime, xFrom + 2, yFrom - 2);
                if (multiCapturesFound.Count() > 0)
                {
                    capturesFound.AddRange(multiCapturesFound);
                }
                else
                {
                    capturesFound.Add(statePrime);
                }
            }
            return capturesFound;
        }

        public static bool MoveIsPossible(Board state, int xFrom, int yFrom, int xTo, int yTo)
        {
            if(xTo < 0 || xTo >= 8 || yTo < 0 || yTo >= 8)//attempted move is out of bounds
                return false;

            if(state.GetPieceAt(xTo, yTo) != Board.Empty)//space is occupied
                return false;

            if ((yTo - yFrom < 0) && (state.GetPieceAt(xFrom, yFrom) != Board.BlackKing))//only kings can move backwards
                return false;

            return true;
        }

        //checks in all 4 directions to see if piece at (xFrom, yFrom) can move in that direction
        //if so, creates a board in that state and adds it to the list
        public static List<Board> GetMovesForPiece(Board state, int xFrom, int yFrom)
        {
            var movesFound = new List<Board>();

            if(MoveIsPossible(state, xFrom, yFrom, xFrom + 1, yFrom + 1))
            {
                sbyte[,] layout = state.CloneBoardState();
                if(yFrom + 1 == 7)//back row reached, make piece into a king
                {
                    layout[xFrom + 1, yFrom + 1] = Board.BlackKing;
                }
                else
                    layout[xFrom + 1, yFrom + 1] = layout[xFrom, yFrom];
                layout[xFrom, yFrom] = Board.Empty;
                movesFound.Add(new Board(layout));
            }
            if (MoveIsPossible(state, xFrom, yFrom, xFrom - 1, yFrom + 1))
            {
                sbyte[,] layout = state.CloneBoardState();
                if (yFrom + 1 == 7)//back row reached, make piece into a king
                {
                    layout[xFrom - 1, yFrom + 1] = Board.BlackKing;
                }
                else
                    layout[xFrom - 1, yFrom + 1] = layout[xFrom, yFrom];
                layout[xFrom, yFrom] = Board.Empty;
                movesFound.Add(new Board(layout));
            }
            if (MoveIsPossible(state, xFrom, yFrom, xFrom - 1, yFrom - 1))
            {
                sbyte[,] layout = state.CloneBoardState();
                layout[xFrom - 1, yFrom - 1] = layout[xFrom, yFrom];
                layout[xFrom, yFrom] = Board.Empty;
                movesFound.Add(new Board(layout));
            }
            if (MoveIsPossible(state, xFrom, yFrom, xFrom + 1, yFrom - 1))
            {
                sbyte[,] layout = state.CloneBoardState();
                layout[xFrom + 1, yFrom - 1] = layout[xFrom, yFrom];
                layout[xFrom, yFrom] = Board.Empty;
                movesFound.Add(new Board(layout));
            }
            return movesFound;
        }

        //Checks if this board has a winner
        public static Winner GetWinner(Board b)
        {
            bool blackFound = false;
            bool redFound = false;
            for(int i = 0; i < 8; i++)
            {
                for(int j = 0; j < 8; j++)
                {
                    if (b.GetPieceAt(i, j) == Board.Black || b.GetPieceAt(i, j) == Board.BlackKing)
                        blackFound = true;
                    else if(b.GetPieceAt(i, j) == Board.Red || b.GetPieceAt(i, j) == Board.RedKing)
                        redFound = true;
                }
            }

            if (redFound && !blackFound)
                return Winner.red;

            if (blackFound && !redFound)
                return Winner.black;

            return Winner.noWinner;
        }
    }

    // This will be used to represent a candidate move by a human player.
    public class MoveSequence
    {
        MoveSequence upLeft { set; get; }
        MoveSequence upRight { set; get; }
        MoveSequence downLeft { set; get; }
        MoveSequence downRight { set; get; }
        Point origin;
        public MoveSequence(int x, int y)
        {
            origin = new Point(x, y);
        }
    }
}
