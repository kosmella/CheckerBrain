using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows.Forms;
using System.IO;

namespace Checkers
{
    public class Trainer:INotifyPropertyChanged
    {
        public Player champion;
        const int trainingInstances = 10;
        const int iterationsPerInstance = 10;
        const int bracketSize = 15;
        const int mutationRate = 10;
        private int _generations;
        private int _gamesPlayed;
        private string _trainingStats;
        private int _winRecord;
        public event PropertyChangedEventHandler PropertyChanged;
        public delegate void NotifyTrainingFinished();
        public event NotifyTrainingFinished TrainingFinished;
        Dispatcher UI;
        public int generations
        {
            get
            {
                return _generations;
            }
        }
        public int gamesPlayed
        {
            get
            {
                return _gamesPlayed;
            }
        }
        public string trainingStats
        {
            get
            {
                return _trainingStats; 
            }
            set
            {
                _trainingStats = value;
                NotifyPropertyChanged("trainingStats");
            }
        }
        public Trainer(Dispatcher UIThreadDispatcher)
        {
            champion = new Player();
            _trainingStats = "Idle";
            _generations = 0;
            _gamesPlayed = 0;
            _winRecord = 0;
            UI = UIThreadDispatcher;
            
        }

        public void Train(CancellationToken cancelNow, CancellationToken cancelAfterGeneration, Player evolveFrom)
        {
            champion = evolveFrom;
            Player challenger;
           Task<Player>[] simTasks = new Task<Player>[trainingInstances];
           Player[] bracket = new Player[trainingInstances];
           while (true)
            {
                simTasks[0] = Task<Player>.Factory.StartNew(() => PlayAndEvolve(champion, cancelNow));
                for (int i = 1; i < trainingInstances; i++)
                {
                    simTasks[i] = Task<Player>.Factory.StartNew(() => PlayAndEvolve(champion.Reproduce(i+1), cancelNow));
                }
                
                for (int i = 0; i < trainingInstances; i++)
                {
                    bracket[i] = simTasks[i].Result;
                }
                /*for (int i = 0; i < trainingInstances; i++)
                {
                    champion = GetWinnerOfGame(champion, bracket[i]);
                }*/
                RunBracket(bracket, trainingInstances);
                challenger = bracket[0];
                for (int i = 1; i < trainingInstances; i++)
                {
                    if(bracket[i].GetWinPercentage() > challenger.GetWinPercentage())
                    {
                        challenger = bracket[i];
                    }
                }
                champion = GetWinnerOfGame(champion, challenger);
                _generations++;
                if (champion.winCount > _winRecord)
                    _winRecord = champion.winCount;
                trainingStats = String.Format("Running . . .\nGeneration: {0}\nGames Played: {1}\nWinner for {2} Games\nRecord streak is {3} wins",
                    generations, gamesPlayed, champion.winCount, _winRecord);
                
                
                if(cancelAfterGeneration.IsCancellationRequested)
                {
                    trainingStats = String.Format("{0} Generations\n{1} Games Played\nWinner for {2} Games", generations, gamesPlayed, champion.winCount);
                    UI.Invoke(TrainingFinished);
                    return;
                }
            }
            
        }
        public Player PlayAndEvolve(Player seed, CancellationToken cancel)
        {
            /*Player challenger = seed.Reproduce(1);
            Player winner = seed;
            for(int i = 0; i < iterationsPerInstance; i++)
            {
                if (cancel.IsCancellationRequested)
                {
                    return winner;
                }
                winner = GetWinnerOfGame(winner, challenger);
                challenger = winner.Reproduce(i + 1);
            }*/
            Player[] bracket = new Player[bracketSize];
            Player first, second, third;
            first = new Player();
            for(int i = 0; i < bracketSize; i++)
            {
                bracket[i] = seed.Reproduce(mutationRate);
            }

            for(int i = 0; i < iterationsPerInstance; i++)
            {
                RunBracket(bracket);
                GetTop3Players(bracket, out first, out second, out third);
                bracket = GenerateBracket(first, second, third);
            }


            return first;
        }

        /*Takes 3 players and generates a new array of Players
        50% are derived from first
        30% derived from second
        20% derived from third
        any remainder slots are also filled with players derived from first
        */
        private Player[] GenerateBracket(Player first, Player second, Player third)
        {
            Player[] bracket = new Player[bracketSize];
            int playersGenerated = 0;
            int firstChildren = (int)(bracketSize * .5);//50% descendents derived from first
            int secondChildren = (int)(bracketSize * .3);//30% from second
            int thirdChildren = (int)(bracketSize * .2);//20% from third
            int remainder = bracketSize - (firstChildren + secondChildren + thirdChildren);

            firstChildren += remainder;//any remaining slots filled with descendents of first

            bracket[0] = first.Reproduce(0);
            bracket[1] = second.Reproduce(0);
            playersGenerated += 2;
            for (int i = 1; i < firstChildren; i++)
            {
                bracket[playersGenerated] = first.Reproduce(mutationRate);
                playersGenerated++;
            }
            for(int i = 1; i < secondChildren; i++)
            {
                bracket[playersGenerated] = second.Reproduce(mutationRate);
                playersGenerated++;
            }
            for (int i = 0; i < thirdChildren; i++)
            {
                bracket[playersGenerated] = third.Reproduce(mutationRate);
                playersGenerated++;
            }
            return bracket;
        }

        //returns the top 3 players by win percentage
        private void GetTop3Players(Player[] bracket, out Player first, out Player second, out Player third)
        {
            //first, second, and third are initialized to the first 3 players in the bracket, and sorted
            first = bracket[0];
            if (bracket[1].GetWinPercentage() > first.GetWinPercentage())
            {
                second = first;
                first = bracket[1];
            }
            else {
                second = bracket[1];
            }

            if (bracket[2].GetWinPercentage() > first.GetWinPercentage())
            {
                third = second;
                second = first;
                first = bracket[2];
            }
            else if (bracket[2].GetWinPercentage() > second.GetWinPercentage())
            {
                third = second;
                second = bracket[2];
            }
            else
                third = bracket[2];

            for(int i = 3; i < bracketSize; i++)
            {
                if(bracket[i].GetWinPercentage() > first.GetWinPercentage())//new first place found
                {
                    third = second;
                    second = first;
                    first = bracket[2];
                }
                else if(bracket[i].GetWinPercentage() > second.GetWinPercentage())//new second place found
                {
                    third = second;
                    second = bracket[i];
                }
                else if(bracket[i].GetWinPercentage() > third.GetWinPercentage())//new third place found
                {
                    third = bracket[i];
                }
            }
        }

        private void RunBracket(Player[] bracket, int size = bracketSize)
        {
            for(int i=0; i < size; i++)
            {
                for(int j = i+1; j < size; j++)
                {
                    GetWinnerOfGame(bracket[i], bracket[j]);
                }
            }
        }

        public Player GetWinnerOfGame(Player redPlayer, Player blackPlayer)
        {
            Winner w;
            Board gameState = new Board();
            redPlayer.isRed = true;
            blackPlayer.isRed = false;
            int turns = 0;
            do
            {
                gameState = GameSim.GetNextMove(gameState, blackPlayer.myBrain, false);
                gameState = GameSim.GetNextMove(gameState, redPlayer.myBrain, true);
                w = gameState.GetWinner();
                turns++;
            }
            while (w == Winner.noWinner && turns < 100);
            Interlocked.Increment(ref _gamesPlayed);
            if(w == Winner.noWinner)//after 100 turns, game is considered a draw. Black player "wins" but neither player gets credit for the win/loss
            {
                return blackPlayer;
            }
            if (w == Winner.black)
            {
                blackPlayer.IncrementWins();
                redPlayer.IncrementLosses();
                return blackPlayer;
            }
            else {
                redPlayer.IncrementWins();
                blackPlayer.IncrementLosses();
                return redPlayer;
            }
        }
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

    }
}
