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
        bool trainerRunning;
        bool trainerStopping;
        CancellationTokenSource cancelNow;
        CancellationTokenSource cancelAfterGeneration;
        Player champion;
        public MainWindow()
        {
            InitializeComponent();
            //image.BringIntoView();
            myTrainer = new Trainer(Dispatcher.CurrentDispatcher);
            myTrainer.TrainingFinished += new Trainer.NotifyTrainingFinished(TrainingFinished);
            this.DataContext = myTrainer;
            trainerRunning = false;
            trainerStopping = false;
            cancelNow = new CancellationTokenSource();
            cancelAfterGeneration = new CancellationTokenSource();
            
        }

        private void EvolveFromScratch_Click(object sender, RoutedEventArgs e)
        {
            if(!trainerRunning && !trainerStopping)
            {
                trainerRunning = true;
                EvolveFromScratch.Content = "Stop After Generation";
                cancelNow = new CancellationTokenSource();
                cancelAfterGeneration = new CancellationTokenSource();
                Task.Run(() => myTrainer.Train(cancelNow.Token, cancelAfterGeneration.Token, new Player()));
                EvolveExisting.IsEnabled = false;
                
            }
            else if(trainerRunning && !trainerStopping)
            {
                trainerStopping = true;
                cancelAfterGeneration.Cancel();
                EvolveFromScratch.Content = "Stop Immediately";
                Results.Text += "\nStopping after generation";
            }
            else if (trainerStopping)
            {
                cancelNow.Cancel();
                EvolveFromScratch.IsEnabled = false;
                EvolveFromScratch.Content = "Stopping...";
                Results.Text = myTrainer.trainingStats;
                
            }
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
            trainerRunning = false;
            trainerStopping = false;
        }

        private void EvolveExisting_Click(object sender, RoutedEventArgs e)
        {
            if (!trainerRunning && !trainerStopping)
            {
                trainerRunning = true;
                EvolveExisting.Content = "Stop After Generation";
                cancelNow = new CancellationTokenSource();
                cancelAfterGeneration = new CancellationTokenSource();
                Player seed = new Player(true);
                Task.Run(() => myTrainer.Train(cancelNow.Token, cancelAfterGeneration.Token, seed));
                EvolveFromScratch.IsEnabled = false;

            }
            else if (trainerRunning && !trainerStopping)
            {
                trainerStopping = true;
                cancelAfterGeneration.Cancel();
                EvolveExisting.Content = "Stop Immediately";
                Results.Text += "\nStopping after generation";
            }
            else if (trainerStopping)
            {
                cancelNow.Cancel();
                EvolveExisting.IsEnabled = false;
                EvolveExisting.Content = "Stopping...";
                Results.Text = myTrainer.trainingStats;

            }
        }
    }
}
