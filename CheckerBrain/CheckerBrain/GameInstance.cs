using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Checkers
{
    //An object of this class is used to represent a game between human and AI or between two AIs
    class GameInstance
    {

        public bool AIvsAI { get; }
        private bool blackTurn;

        private CheckerBrain redPlayer, blackPlayer;
        public Board boardState { get
            {
                return _boardState;
            } }
        private Board _boardState;
        public GameInstance(Player opponent)
        {
            AIvsAI = false;
            blackTurn = true;
            _boardState = new Board();
        }

        public GameInstance(CheckerBrain black, CheckerBrain red)
        {
            AIvsAI = true;
            blackTurn = true;
            redPlayer = red;
            blackPlayer = black;
            
            _boardState = new Board();
        }

        public void AIMove()
        {
            if (blackTurn && !AIvsAI)
                throw new InvalidOperationException("AIMove method called when it is not AI's turn");

            if (blackTurn)
            {
                _boardState = GameSim.GetNextMove(boardState, blackPlayer, false);
            }
            else
            {
                _boardState = GameSim.GetNextMove(boardState, redPlayer, true);
            }
            blackTurn = !blackTurn;
        }

        public void HumanTurn(Board newBoardState)
        {
            if (AIvsAI)
                throw new InvalidOperationException("HumanTurn method called in AI vs AI game");

            if (!blackTurn)
                throw new InvalidOperationException("HumanTurn method called when it is AI's turn");

            _boardState = newBoardState;
            blackTurn = false;
        }

        public Winner GetWinner()
        {
            return _boardState.GetWinner();
        }
    }
}
