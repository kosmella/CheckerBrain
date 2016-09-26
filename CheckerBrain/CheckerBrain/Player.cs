using System.IO;
using System.Windows.Forms;

namespace Checkers
{
    public class Player
    {
        public CheckerBrain myBrain { get; }
        public bool isRed { get; set; }
        public int winCount { get; set; }
        public int lossCount { get; set; }
        public int gamesPlayed { get; set; }

        public Player(bool loadFromFile = false)
        {
            myBrain = new CheckerBrain(loadFromFile);
            winCount = 0;
            lossCount = 0;
            gamesPlayed = 0;
        }
        public Player(CheckerBrain brain)
        {
            myBrain = brain;
            winCount = 0;
            lossCount = 0;
            gamesPlayed = 0;
        }
        public Player Reproduce(int mutationRate)
        {
            Player child = new Player(new CheckerBrain(myBrain));
            if (mutationRate > 0)
                child.myBrain.Mutate(mutationRate);
            return child;
        }

        public float GetWinPercentage()
        {
            if (gamesPlayed == 0)
                return 0;
            else
                return (float)winCount / (float)gamesPlayed;
        }
        public void IncrementWins()
        {
            winCount++;
        }
        public void IncrementLosses()
        {
            lossCount++;
        }
        
        //Saves the state of the neural network to a file.
        public void Save()
        {
            SaveFileDialog sf = new SaveFileDialog();
            sf.Filter = "brain files (*.brain)|*.brain";
            if (sf.ShowDialog() == DialogResult.OK || sf.FileName != "")
            {
                using (StreamWriter sw = new StreamWriter(sf.FileName))
                {
                    sw.WriteLine(myBrain.ToString());
                }
            }
        }
    }
}
