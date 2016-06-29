using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace Checkers
{
    public class CheckerBrain : NeuralNet
    {
        const int inputsFromBoard = 32;
        public CheckerBrain(bool loadFromFile = false)
        {
            if (loadFromFile)
            {
                Load();
            }
        }
        
        //For checkers, input size must be 32 and output size must be 1
        //This constructor creates a neural net with 32 inputs and 1 output, with intermediateLayerSizes specifying the neurons in each intermediate layer
        public CheckerBrain(params int[] intermediateLayerSizes)
        {
            int[] layerSizes = new int[intermediateLayerSizes.Length + 2];
            layerSizes[0] = inputsFromBoard;
            layerSizes[layerSizes.Length - 1] = 1;
            for(int i = 1; i < layerSizes.Length - 1; i++)
            {
                layerSizes[i] = intermediateLayerSizes[i - 1];
            }
            BuildNetwork(layerSizes);
        }

        
        //returns a new CheckerBrain with the same synaptic weights
        public CheckerBrain(CheckerBrain parent)
            : base(parent)
        {
            
        }    

        // Iterates through each candidate next move using foreach loop.
        // Runs evaluation function on each candidate and returns the one with the highest assed value.
        public Board DecideNextMove(List<Board> possibleMoves) {
            if (possibleMoves.Count == 1)
                return possibleMoves[0];
            float assessmentOfBoard = -100f;
            Board best = possibleMoves[0];
            foreach(Board b in possibleMoves)
            {
                float temp = Evaluate(b.ConvertToInputVector());
                if(temp > assessmentOfBoard)
                {
                    assessmentOfBoard = temp;
                    best = b;
                }
            }
            return best;
        } 

    }
}
