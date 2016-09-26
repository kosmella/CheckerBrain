namespace Checkers
{
    struct Point
    {   
        public int x { get; }
        public int y { get; }
        public Point(int x, int y)
        {
            this.x = x;
            this.y = y;
            
        }
        bool equals(Point p)
        {
            return ((p.x == this.x)&&(p.y == this.y));
        }
    }
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

        //Tests to see if game has a winner yet
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
        
        //Makes a piece into a king if it reached opponent's back row
        public void MakeKing(int x, int y)
        {
            if ((y == 7) && boardState[x, y] == Black)
                boardState[x, y] = BlackKing;

            else if ((y == 0) && boardState[x, y] == Red)
                boardState[x, y] = RedKing;
        }

    }
}
