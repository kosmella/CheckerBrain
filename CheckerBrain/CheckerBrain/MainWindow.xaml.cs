using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Checkers
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
    public partial class MainWindow : Window
    {
        Trainer myTrainer;
        CancellationTokenSource cancelNow;
        CancellationTokenSource cancelAfterGeneration;
        Player champion;
        GameInstance currentGame;
        Dictionary<Canvas, Point> BoardUI;  //associates each rectange of the checkerboard UI to a Point (x, y coordinates stored in a struct)
        const int SpaceWidth = 30;
        const int SpaceHeight = 30;
        private state ProgramState;
        enum state { Idle, TrainerRunning, TrainerStopping, AwaitingPlayer, AwaitingAI, AIvsAI}
        public MainWindow()
        {
            InitializeComponent();
            DrawBoard(new Board());
            ProgramState = state.Idle;
        }

        private void Evolve_Click(object sender, RoutedEventArgs e)
        {
            if(ProgramState == state.Idle)
            {
                Button b = sender as Button;
                if (b == null)
                    return;
                bool loadFromFile = (b == EvolveExisting);
                StartTraining(loadFromFile);
                return;
            }
            else if(ProgramState == state.TrainerRunning)
            {
                ProgramState = state.TrainerStopping;
                cancelAfterGeneration.Cancel();
                EvolveFromScratch.Content = "Stop Immediately";
                Results.Text += "\nStopping after generation";
                return;
            }
            else if (ProgramState == state.TrainerStopping)
            {
                cancelNow.Cancel();
                EvolveFromScratch.IsEnabled = false;
                EvolveFromScratch.Content = "Stopping...";
                Results.Text = myTrainer.trainingStats;
                return; 
            }
        }

        private void StartTraining(bool loadFile = false)
        {
            ProgramState = state.TrainerRunning;
            EvolveFromScratch.Content = "Stop After Generation";
            myTrainer = new Trainer(Dispatcher.CurrentDispatcher);
            myTrainer.TrainingFinished += new Trainer.NotifyTrainingFinished(TrainingFinished);
            this.DataContext = myTrainer;
            cancelNow = new CancellationTokenSource();
            cancelAfterGeneration = new CancellationTokenSource();
            Player seed = new Player(loadFile);
            Task.Run(() => myTrainer.Train(cancelNow.Token, cancelAfterGeneration.Token, seed));
            EvolveExisting.IsEnabled = false;
            Results.Text = "Trainer starting...";
            return;
        }
        private void TrainingFinished()
        {
            champion = myTrainer.champion;
            champion.Save();
            Results.Text = myTrainer.trainingStats;
            EvolveFromScratch.Content = "Evolve From Scratch";
            EvolveExisting.Content = "Evolve Existing";
            EvolveExisting.IsEnabled = true;
            EvolveFromScratch.IsEnabled = true;
            ProgramState = state.Idle;
        }

        

        private void PlayAI_Click(object sender, RoutedEventArgs e)
        {
            if(ProgramState == state.Idle)
            {
                InitializeAIGame();
                ProgramState = state.AIvsAI;
                Step.IsEnabled = true;
                PlayAI.Content = "Abort Game";
                return;
            }
            else if (ProgramState == state.AIvsAI)
            {
                ProgramState = state.Idle;
                PlayAI.Content = "Play AI";
                Step.IsEnabled = false;
            }
        }

        private void InitializeAIGame()
        {
            DrawBoard(new Board());
            CheckerBrain red, black;
            red = new CheckerBrain(true);
            black = new CheckerBrain(true);
            currentGame = new GameInstance(black, red);
        }

        //Builds a dictionary with each rectangle of the board UI as key, and a point representing its coordinates as the value
        private void InitializeBoardUI()
        {
            BoardUI = new Dictionary<Canvas, Point>();
            BoardUI.Add(x0y0, new Point(0, 0));
            BoardUI.Add(x2y0, new Point(2, 0));
            BoardUI.Add(x4y0, new Point(4, 0));
            BoardUI.Add(x6y0, new Point(6, 0));
            BoardUI.Add(x0y2, new Point(0, 2));
            BoardUI.Add(x2y2, new Point(2, 2));
            BoardUI.Add(x4y2, new Point(4, 2));
            BoardUI.Add(x6y2, new Point(6, 2));
            BoardUI.Add(x0y4, new Point(0, 4));
            BoardUI.Add(x2y4, new Point(2, 4));
            BoardUI.Add(x4y4, new Point(4, 4));
            BoardUI.Add(x6y4, new Point(6, 4));
            BoardUI.Add(x0y6, new Point(0, 6));
            BoardUI.Add(x2y6, new Point(2, 6));
            BoardUI.Add(x4y6, new Point(4, 6));
            BoardUI.Add(x6y6, new Point(6, 6));
            BoardUI.Add(x1y1, new Point(1, 1));
            BoardUI.Add(x1y3, new Point(1, 3));
            BoardUI.Add(x1y5, new Point(1, 5));
            BoardUI.Add(x1y7, new Point(1, 7));
            BoardUI.Add(x3y1, new Point(3, 1));
            BoardUI.Add(x3y3, new Point(3, 3));
            BoardUI.Add(x3y5, new Point(3, 5));
            BoardUI.Add(x3y7, new Point(3, 7));
            BoardUI.Add(x5y1, new Point(5, 1));
            BoardUI.Add(x5y3, new Point(5, 3));
            BoardUI.Add(x5y5, new Point(5, 5));
            BoardUI.Add(x5y7, new Point(5, 7));
            BoardUI.Add(x7y1, new Point(7, 1));
            BoardUI.Add(x7y3, new Point(7, 3));
            BoardUI.Add(x7y5, new Point(7, 5));
            BoardUI.Add(x7y7, new Point(7, 7));
        }

        private void DrawBoard(Board b)
        {
            if (BoardUI == null)
                InitializeBoardUI();

            Point location;
            for(int i=0; i<8; i++)
            {
                for(int j=0; j<8; j++)
                {
                    
                        location = new Point(i, j);
                        Canvas rectangleFound = BoardUI.Where(r => r.Value.Equals(location)).Select(r => r.Key).FirstOrDefault();
                        //rectangleFound.Children.Add(GetPieceGraphic(b.GetPieceAt(i, j)));
                        DrawBoardSpace(rectangleFound, b.GetPieceAt(i, j));
                    
                }
            }
        }

        private void BoardClicked(object sender, MouseButtonEventArgs e)
        {
            /*if(BoardUI == null)
                InitializeBoardUI();
            Canvas spaceClicked = sender as Canvas;
            if (spaceClicked == null)
                return;
            Point pointClicked = BoardUI[spaceClicked];
            Results.Text = String.Format("{0}, {1}", pointClicked.x, pointClicked.y);
            if(spaceClicked.Children.Count == 0)
                spaceClicked.Children.Add(GetPieceGraphic(Board.Red));
            else
            {
                spaceClicked.Children.Clear();
            }
            */
        }

        private void DrawBoardSpace(Canvas space, sbyte pieceType)
        {
            if (space == null)
                return;
            space.Children.Clear();
            if (pieceType == Board.Empty)
                return;

            Ellipse piece = new Ellipse();
            piece.Width = SpaceWidth;
            piece.Height = SpaceHeight;
            if(pieceType == Board.Black || pieceType == Board.BlackKing)
            {
                piece.Fill = new SolidColorBrush(Colors.Black);
            }
            else if(pieceType == Board.Red || pieceType == Board.RedKing)
            {
                piece.Fill = new SolidColorBrush(Colors.Red);
            }
            space.Children.Add(piece);
            if(pieceType == Board.BlackKing || pieceType == Board.RedKing)
            {
                TextBlock kingText = new TextBlock();
                kingText.Foreground = new SolidColorBrush(Colors.Yellow);
                kingText.Text = "K";
                space.Children.Add(kingText);
            }
        }

        private void Step_Click(object sender, RoutedEventArgs e)
        {
            currentGame.AIMove();
            DrawBoard(currentGame.boardState);
            if(currentGame.GetWinner() == Winner.black)
            {
                Results.Text = "Black wins";
                ProgramState = state.Idle;
                PlayAI.Content = "Play AI";
                Step.IsEnabled = false;
            }
            else if(currentGame.GetWinner() == Winner.red)
            {
                    Results.Text = "Red wins";
                    ProgramState = state.Idle;
                    PlayAI.Content = "Play AI";
                    Step.IsEnabled = false;
                
            }
        }
    }
}
