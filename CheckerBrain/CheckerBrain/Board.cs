using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Checkers
{
    public class Board
    {
        public const sbyte BlackKing = 15;
        public const sbyte Black = 10;
        public const sbyte Empty = 0;
        public const sbyte Red = -10;
        public const sbyte RedKing = -15;

        private sbyte[,] boardState; //2D array of pieces
        public Board()// creates a new board using default layout
        {
            initializeNewBoard();
        }
        public Board(sbyte[,] state) //creates a new board in the state specified
        {
            boardState = state;
           
        }
        public sbyte[,] CloneBoardState()
        {
            sbyte[,] clone = new sbyte[8, 8];
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                    clone[i, j] = this.GetPieceAt(i, j);
            return clone;
        }

        //initializes the board for a new game with default piece layout
        public void initializeNewBoard()
        {
            boardState = new sbyte[8, 8] { 
                { Black, Empty, Black, Empty, Empty, Empty, Red, Empty},
                { Empty, Black, Empty, Empty, Empty, Red, Empty, Red},
                { Black, Empty, Black, Empty, Empty, Empty, Red, Empty},
                { Empty, Black, Empty, Empty, Empty, Red, Empty, Red},
                { Black, Empty, Black, Empty, Empty, Empty, Red, Empty},
                { Empty, Black, Empty, Empty, Empty, Red, Empty, Red},
                { Black, Empty, Black, Empty, Empty, Empty, Red, Empty},
                { Empty, Black, Empty, Empty, Empty, Red, Empty, Red}
            };
        }
        //Each CheckerBrain works under the assumption that it is playing as Black.
        //This function inverts the board by doing a 180 degree rotation and changing all blacks to reds and vice versa.
        public void Invert()
        {
            sbyte temp;
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    temp = boardState[i, j];
                    boardState[i, j] = (sbyte)(-1 * boardState[7 - i, 7 - j]);
                    boardState[7 - i, 7 - j] = (sbyte)(-1 * temp);

                }
            }
        }

        //Converts the state of the board to a 1-dimensional vector for input to the neural network
        public float[] ConvertToInputVector()
        {
            int inputSlot = 0;
            float[] inputVector = new float[32];
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if ((i + j) % 2 == 0)
                    {
                        inputVector[inputSlot++] = GetPieceAt(i, j);
                    }
                }
            }
            return inputVector;
        }

        public List<Board> GetPossibleMoves()
        {
            //if a capture is possible, a player MUST take it.
            List<Board> moves = new List<Board>();

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    sbyte currentPiece = GetPieceAt(i, j);
                    if (currentPiece == Board.Black || currentPiece == Board.BlackKing)
                    {
                        moves.AddRange(GetCapturesForPiece(i, j));
                    }


                }
            }

            if (moves.Count() > 0) //captures possible, so return ONLY those
                return moves;

            //No captures are possible so start looking at moves
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    sbyte currentPiece = GetPieceAt(i, j);
                    if (currentPiece == Board.Black || currentPiece == Board.BlackKing)
                    {
                        moves.AddRange(GetMovesForPiece(i, j));
                    }


                }
            }
            return moves;
        }

        //Recursive method that returns a list of boards representing all the captures that can be made by piece at (xFrom, yFrom)
        //Check if jump is possible in up/right direction
        //If jump possible, create a new board to represent the state after that jump was made, call that statePrime
        //Call GetCapturesForPiece on statePrime (checking for multi-captures)
        //If that list has size >0, add it to capturesFound
        //Else add boardPrime to capturesFound (if a multi-capture is possible it MUST be made, so only add boardPrime if no further jumps possible)
        //repeat for other 3 directions
        //return capturesFound
        public List<Board> GetCapturesForPiece(int xFrom, int yFrom)
        {
            var capturesFound = new List<Board>();
            var multiCapturesFound = new List<Board>();
            Board statePrime;
            if (CaptureIsPossible(xFrom, yFrom, xFrom + 2, yFrom + 2))
            {
                //capture is possibe in up/right direction.  Clone board's state and modify it
                sbyte[,] layout = CloneBoardState();

                //move piece at (xFrom, yFrom) to (xFrom+2, yFrom+2)
                layout[xFrom + 2, yFrom + 2] = layout[xFrom, yFrom];
                layout[xFrom + 1, yFrom + 1] = Board.Empty;
                layout[xFrom, yFrom] = Board.Empty;
                statePrime = new Board(layout);
                multiCapturesFound = statePrime.GetCapturesForPiece(xFrom + 2, yFrom + 2);
                if (multiCapturesFound.Count() > 0)
                {
                    capturesFound.AddRange(multiCapturesFound);
                }
                else
                {
                    if (yFrom + 2 == 7)//piece lands on the last row and becomes a king
                    {
                        layout[xFrom + 2, yFrom + 2] = Board.BlackKing;
                        statePrime = new Board(layout);
                    }
                    capturesFound.Add(statePrime);
                }
            }
            if (CaptureIsPossible(xFrom, yFrom, xFrom - 2, yFrom + 2))
            {
                //capture is possibe in up/left direction.  Clone board's state and modify it
                sbyte[,] layout = CloneBoardState();

                //move piece at (xFrom, yFrom) to (xFrom-2, yFrom+2)
                layout[xFrom - 2, yFrom + 2] = layout[xFrom, yFrom];
                layout[xFrom - 1, yFrom + 1] = Board.Empty;
                layout[xFrom, yFrom] = Board.Empty;
                statePrime = new Board(layout);
                multiCapturesFound = statePrime.GetCapturesForPiece(xFrom - 2, yFrom + 2);
                if (multiCapturesFound.Count() > 0)
                {
                    capturesFound.AddRange(multiCapturesFound);
                }
                else
                {
                    if (yFrom + 2 == 7)//piece lands on the last row and becomes a king
                    {
                        layout[xFrom - 2, yFrom + 2] = Board.BlackKing;
                        statePrime = new Board(layout);
                    }
                    capturesFound.Add(statePrime);
                }
            }
            if (CaptureIsPossible(xFrom, yFrom, xFrom - 2, yFrom - 2))
            {
                //capture is possibe in down/left direction.  Clone board's state and modify it
                sbyte[,] layout = CloneBoardState();

                //move piece at (xFrom, yFrom) to (xFrom-2, yFrom-2)
                layout[xFrom - 2, yFrom - 2] = layout[xFrom, yFrom];
                layout[xFrom - 1, yFrom - 1] = Board.Empty;
                layout[xFrom, yFrom] = Board.Empty;
                statePrime = new Board(layout);
                multiCapturesFound = statePrime.GetCapturesForPiece(xFrom - 2, yFrom - 2);
                if (multiCapturesFound.Count() > 0)
                {
                    capturesFound.AddRange(multiCapturesFound);
                }
                else
                {
                    capturesFound.Add(statePrime);
                }
            }
            if (CaptureIsPossible(xFrom, yFrom, xFrom + 2, yFrom - 2))
            {
                //capture is possibe in down/right direction.  Clone board's state and modify it
                sbyte[,] layout = CloneBoardState();

                //move piece at (xFrom, yFrom) to (xFrom+2, yFrom-2)
                layout[xFrom + 2, yFrom - 2] = layout[xFrom, yFrom];
                //piece that got jumped is removed from the board
                layout[xFrom + 1, yFrom - 1] = Board.Empty;
                //origin of the piece that made the jump is now empty
                layout[xFrom, yFrom] = Board.Empty;
                statePrime = new Board(layout);
                multiCapturesFound = statePrime.GetCapturesForPiece(xFrom + 2, yFrom - 2);
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

        //Determines if piece at (xFrom, yFrom) can make a capture by jumping to (xTo, yTo)
        public bool CaptureIsPossible(int xFrom, int yFrom, int xTo, int yTo)
        {
            if (xFrom < 0 || xFrom >= 8 || yFrom < 0 || yFrom >= 8)
            {
                Console.WriteLine("tried to move piece at {0}, {1}", xFrom, yFrom);
            }
            if (xTo < 0 || xTo >= 8 || yTo < 0 || yTo >= 8)//proposed move is out of bounds
                return false;
            if (GetPieceAt(xTo, yTo) != Board.Empty)//destination is not empty, capture cannot be made
                return false;
            if (yTo < yFrom && GetPieceAt(xFrom, yFrom) != Board.BlackKing)//non-king can't move backwards
                return false;
            if (GetPieceAt((xFrom + xTo) / 2, (yFrom + yTo) / 2) != Board.Red &&
                GetPieceAt((xFrom + xTo) / 2, (yFrom + yTo) / 2) != Board.RedKing)//space being jumped does not contain enemy piece
                return false;
            return true;
        }

        public bool MoveIsPossible(int xFrom, int yFrom, int xTo, int yTo)
        {
            if (xTo < 0 || xTo >= 8 || yTo < 0 || yTo >= 8)//attempted move is out of bounds
                return false;

            if (GetPieceAt(xTo, yTo) != Board.Empty)//space is occupied
                return false;

            if ((yTo - yFrom < 0) && (GetPieceAt(xFrom, yFrom) != Board.BlackKing))//only kings can move backwards
                return false;

            return true;
        }

        //checks in all 4 directions to see if piece at (xFrom, yFrom) can move in that direction
        //if so, creates a board in that state and adds it to the list
        public List<Board> GetMovesForPiece(int xFrom, int yFrom)
        {
            var movesFound = new List<Board>();
            if (MoveIsPossible(xFrom, yFrom, xFrom + 1, yFrom + 1))
            {
                sbyte[,] layout = CloneBoardState();
                if (yFrom + 1 == 7)//back row reached, make piece into a king
                {
                    layout[xFrom + 1, yFrom + 1] = Board.BlackKing;
                }
                else
                    layout[xFrom + 1, yFrom + 1] = layout[xFrom, yFrom];
                layout[xFrom, yFrom] = Board.Empty;
                movesFound.Add(new Board(layout));
            }
            if (MoveIsPossible(xFrom, yFrom, xFrom - 1, yFrom + 1))
            {
                sbyte[,] layout = CloneBoardState();
                if (yFrom + 1 == 7)//back row reached, make piece into a king
                {
                    layout[xFrom - 1, yFrom + 1] = Board.BlackKing;
                }
                else
                    layout[xFrom - 1, yFrom + 1] = layout[xFrom, yFrom];
                layout[xFrom, yFrom] = Board.Empty;
                movesFound.Add(new Board(layout));
            }
            if (MoveIsPossible(xFrom, yFrom, xFrom - 1, yFrom - 1))
            {
                sbyte[,] layout = CloneBoardState();
                layout[xFrom - 1, yFrom - 1] = layout[xFrom, yFrom];
                layout[xFrom, yFrom] = Board.Empty;
                movesFound.Add(new Board(layout));
            }
            if (MoveIsPossible(xFrom, yFrom, xFrom + 1, yFrom - 1))
            {
                sbyte[,] layout = CloneBoardState();
                layout[xFrom + 1, yFrom - 1] = layout[xFrom, yFrom];
                layout[xFrom, yFrom] = Board.Empty;
                movesFound.Add(new Board(layout));
            }
            return movesFound;
        }
        public sbyte GetPieceAt(int x, int y)
        {
            return boardState[x, y];
        }
        public string toString()
        {
            string s = "";
            for(int j = 7; j >= 0; j--)
            {
                for (int i = 0; i < 8; i++)
                {
                    s += GetPieceAt(i, j);
                }
                s += "\n";
            }
            return s;
        }
        public Winner GetWinner()
        {
            bool blackFound = false;
            bool redFound = false;
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (GetPieceAt(i, j) == Black || GetPieceAt(i, j) == BlackKing)
                        blackFound = true;
                    else if (GetPieceAt(i, j) == Red || GetPieceAt(i, j) == RedKing)
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
}
