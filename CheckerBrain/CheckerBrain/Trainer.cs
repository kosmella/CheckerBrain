using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Runtime.CompilerServices;

namespace Checkers
{
    public class Trainer:INotifyPropertyChanged
    {   
        const int trainingTasks = 10;
        const int iterationsPerTask = 10;
        const int playersPerBracket = 15;
        const int mutationRate = 10;

        private string _trainingStats, trainerStatus;
        private int _gamesPlayed;
        public event PropertyChangedEventHandler PropertyChanged;
        public delegate void NotifyTrainingFinished();
        public event NotifyTrainingFinished TrainingFinished;
        Dispatcher UI;
        public Player champion { get; private set; }
        public int generations { get; private set; }
        public int winRecord { get; private set; }
        public int generationsSinceNewChampion { get; private set; }

        public string trainingStats //data binding path for UI's status window
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
            trainerStatus = "Idle";   
            UI = UIThreadDispatcher;  //used to call TrainingFinished method on UI thread
        }

        /* Training algorithm
         *
         * Algorithm works as follows:
         * several tasks run the PlayAndEvolve method (# tasks = trainingTasks)
         * PlayAndEvolve takes a seed player, fills an array (size = playersPerBracket) with mutated copies of the seed,
         * and has every player play all the others.  The top 3 are used as seeds for the next iteration.  This process
         * is repeated iterationsPerTask times, and then the top player is returned as the result of the task.
         * Once every training task completes, each player plays all the others, and the one with the most wins
         * becomes the challenger.  The challenger plays the existing champion and, if it wins, becomes the
         * new champion.
         * The process is repeated in an infinite loop 
         *
         * cancelAfterGeneration waits for all tasks in simTasks to complete and then computes the champion
         * cancelNow causes all the simTasks to abort before completing all their iterations
         */
        public void Train(CancellationToken cancelNow, CancellationToken cancelAfterGeneration, Player seed)
        {
            generationsSinceNewChampion = 0;
            generations = 0;
            _gamesPlayed = 0;
            winRecord = 0;
            champion = seed;
            Player challenger;
            Task notifier = Task.Factory.StartNew(() => UpdateStats(cancelAfterGeneration)); //this task updates the trainingStats property at fixed intervals
            Task<Player>[] simTasks = new Task<Player>[trainingTasks];
            Player[] bracket = new Player[trainingTasks];
            trainerStatus = "Running. . .";

            //first element is seeded with the champion, the remaining are seeded with mutations of the champion
            simTasks[0] = Task<Player>.Factory.StartNew(() => PlayAndEvolve(champion, cancelAfterGeneration));
            for (int i = 1; i < trainingTasks; i++)
            {
                simTasks[i] = Task<Player>.Factory.StartNew(() => PlayAndEvolve(champion.Reproduce(i + 1), cancelNow));
            }
            while (true)
            {
                /*The main training loop works as follows
                 * Wait for all PlayAndEvolve tasks to complete.
                 * Create a new bracket with the result of each task.
                 * Call RunBracket method to have each of them play all the others
                 * Have the top player from that bracket play against the reigning champion,
                 * and make it the new champion if it wins.
                 * If cancellation token was cancelled, end the training loop.
                 * Else, begin new generation of PlayAndEvolve tasks with the results of the prior generation.
                 */
                Task.WaitAll(simTasks); 
                for (int i = 0; i < trainingTasks; i++)
                {
                    bracket[i] = simTasks[i].Result;
                }             
                RunBracket(bracket, trainingTasks);
                challenger = GetTopPlayer(bracket);
                champion = GetWinnerOfGame(challenger, champion);
                generations++;
                if (champion != challenger) generationsSinceNewChampion = 0;
                else
                {
                    generationsSinceNewChampion++;
                    if (generationsSinceNewChampion > winRecord) winRecord = generationsSinceNewChampion;
                }
                if (cancelAfterGeneration.IsCancellationRequested)
                {
                    trainerStatus = "Finished";
                    trainingStats = String.Format("{0}\nGeneration: {1}\nGames Played: {2}\nChamp for {3} generations\nRecord is {4} generations",
                    trainerStatus, generations, _gamesPlayed, generationsSinceNewChampion, winRecord);
                    UI.Invoke(TrainingFinished);
                    return;
                }
                for (int i = 0; i < trainingTasks; i++)
                {
                    Player p = bracket[i].Reproduce(0);
                    simTasks[i] = Task<Player>.Factory.StartNew(() => PlayAndEvolve(p, cancelNow));
                }
            }
        }

        //Updates the trainingStats string at fixed intervals (default = 1000 ms)
        //terminates when cancel token is cancelled
        private void UpdateStats(CancellationToken cancel, int tickRateInMilliseconds = 1000)
        {
            while (true)
            {
                if (cancel.IsCancellationRequested)
                {
                    trainerStatus = "Finishing generation. . .";
                }
                if (trainerStatus == "Finished")
                    return;

                trainingStats = String.Format("{0}\nGeneration: {1}\nGames Played: {2}\nChamp for {3} generations\nRecord is {4} generations",
                    trainerStatus, generations, _gamesPlayed, generationsSinceNewChampion, winRecord);
                Thread.Sleep(tickRateInMilliseconds);
            }
        }

        /*creates a bracket of players, has them each play all the others, and
         * creates a new bracket seeded by the top 3.  Repeats iterationsPerTask times.
        */
        public Player PlayAndEvolve(Player seed, CancellationToken cancel)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;         
            Player[] bracket = new Player[playersPerBracket];
            Player first, second, third;
            first = seed;
            bracket[0] = seed;
            for(int i = 1; i < playersPerBracket; i++)
            {
                bracket[i] = seed.Reproduce(mutationRate);
            }
            for(int i = 0; i < iterationsPerTask; i++)
            {
                if (cancel.IsCancellationRequested)
                    return first;
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
            Player[] bracket = new Player[playersPerBracket];
            int playersGenerated = 0;
            int firstChildren = (int)(playersPerBracket * .5);//50% descendents derived from first
            int secondChildren = (int)(playersPerBracket * .3);//30% from second
            int thirdChildren = (int)(playersPerBracket * .2);//20% from third
            int remainder = playersPerBracket - (firstChildren + secondChildren + thirdChildren);

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
        
        //Returns the player with the best win percentage
        private Player GetTopPlayer(Player[] players)
        {
            Player top = players[0];
            foreach(Player p in players)
            {
                if(p.GetWinPercentage() > top.GetWinPercentage())
                {
                    top = p;
                }
            }
            return top;
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

            for(int i = 3; i < playersPerBracket; i++)
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

        //Makes each player in bracket play all the others
        private void RunBracket(Player[] bracket, int size = playersPerBracket)
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
        private void NotifyPropertyChanged([CallerMemberName] String info ="")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

    }
}
