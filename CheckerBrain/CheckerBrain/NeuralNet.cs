using System;
using System.IO;
using System.Windows.Forms;

namespace Checkers
{
    public class NeuralNet
    {
        Layer[] layers;
        private static readonly int[] defaultConfig = { 32, 100, 24, 1 };
        public int numLayers { get { return layers.Length; } }
        public NeuralNet(params int[] layerSizes)
        {
            if (layerSizes.Length == 0)
                BuildNetwork(defaultConfig);
            else
                BuildNetwork(layerSizes);  //initialize variables in a separate method so it can be called from a subclass with modified parameters
        }
        public NeuralNet(NeuralNet cloneMe)
        {
            layers = new Layer[cloneMe.numLayers];
            for(int i = 0; i < layers.Length; i++)
            {
                layers[i] = cloneMe.layers[i].Clone();
            }
            
        }

        //this initializes each layer in the network
        protected void BuildNetwork(params int[] layerSizes)
        {
            layers = new Layer[layerSizes.Length - 1];
            for (int i = 0; i < layers.Length; i++)
            {
                layers[i] = new Layer(layerSizes[i], layerSizes[i + 1]);
            }
        }

        //takes an input vector and outputs the result of the evaluation function
        //throws an ArgumentException if inputVector length is not the same as the # of inputs in the first layer of the network
        public float Evaluate(float[] inputVector)
        {
            if (inputVector.Length != layers[0].inputs)
                throw new System.ArgumentException("wrong number of inputs for this network");
            float[] outputOfPreviousLayer = inputVector;
            for(int i = 0; i < layers.Length; i++)
            {
                outputOfPreviousLayer = layers[i].ProcessInput(outputOfPreviousLayer);
            }
            return outputOfPreviousLayer[0];
        }

        //Mutates each layer according to mutationRate
        //mutationRate=1 equivalent to 1/1000 chance any given synapse will be modified
        public void Mutate(int mutationRate)
        {
            foreach (Layer l in layers)
                l.Mutate(mutationRate);
        }
        
        
        //Creates string representation of network intended to output to a save file
        //First line of output shows # of layers
        //Next lines show input/output sizes of each layer (same values as params provided to the constructor)
        //Next lines show all the synaptic weights, one per line
        public override string ToString() {
            string reply = "";
            reply += numLayers + Environment.NewLine;
            reply += layers[0].inputs + Environment.NewLine;
            foreach(Layer l in layers)
            {
                reply += l.outputs + Environment.NewLine;
            }
            foreach(Layer l in layers)
            {
                reply += l.ToString();
            }
            return reply;
        }

        //Initializes a neural net using values in a file
        //Values are loaded in the same order they are printed by the ToString() method
       //Initializes the network with random values if a file is not specified
       //TODO: modify this method to return a bool representing whether load was successful
        protected void Load()
        {
            Stream input = null;
            OpenFileDialog open = new OpenFileDialog();
            open.Filter = "brain files (*.brain)|*.brain";

            if (open.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if ((input = open.OpenFile()) != null)
                    {
                        using (input)
                        {
                            using (StreamReader inputStream = new StreamReader(input))
                            {
                                int numLayers = int.Parse(inputStream.ReadLine());
                                layers = new Layer[numLayers];
                                int[] layerSizes = new int[numLayers + 1]; //used to read and store input/output size for each layer
                                for (int i = 0; i < numLayers + 1; i++)
                                {
                                    layerSizes[i] = int.Parse(inputStream.ReadLine());
                                }
                                for (int i = 0; i < numLayers; i++)
                                {
                                    layers[i] = new Layer(layerSizes[i], layerSizes[i + 1]);
                                }
                                foreach (Layer l in layers)
                                {
                                    for (int neuron = 0; neuron < l.inputs; neuron++)
                                    {
                                        for (int synapse = 0; synapse < l.outputs; synapse++)
                                        {
                                            l.SetWeight(neuron, synapse, float.Parse(inputStream.ReadLine()));
                                        }
                                    }
                                }
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error reading values from file " + ex.Message);
                    throw new Exception("failed to load from file");
                }
            }
            else BuildNetwork(defaultConfig);//User cancelled without loading a file, uses a randomized network instead
        }
        class Layer
        {
            //TODO: change these to properties
            public int inputs;
            public int outputs;
            float[,] synapses;
            const float maxWeight = 10; //max synaptic weight value
            const float maxDelta = 3; //max change if a synapse mutates
            //public constructor that creates a randomized set of synapses with specified # of inputs and outputs
            public Layer(int inputs, int outputs)
            {
                this.inputs = inputs;
                this.outputs = outputs;
                synapses = new float[inputs, outputs];
                System.Threading.Thread.Sleep(10);  //this avoids two neural nets using the same seed

                //randomize all synaptic weights
                Random rand = new Random();
                //float nextWeight;
                for (int i = 0; i < inputs; i++)
                {
                    for (int j = 0; j < outputs; j++)
                    {
                        synapses[i, j] = ((float)rand.NextDouble() * maxWeight * 2) - maxWeight; //formuala for random # between maxWeight and -maxWeight
                    }

                }
            }

            //private constructor intended to be called by the Clone method
            private Layer(int inputs, int outputs, float[,] synapses)
            {
                this.synapses = synapses;
                this.inputs = inputs;
                this.outputs = outputs;
            }

            public void SetWeight(int neuron, int synapse, float weight)
            {
                synapses[neuron, synapse] = weight;
            }

            //creates a deep copy of this layer then mutates it according to mutationRate
            public Layer Clone(int mutationRate = 0)
            {
                float[,] clonedSynapses = new float[inputs, outputs];
                for(int i = 0; i < inputs; i++)
                {
                    for(int j = 0; j < outputs; j++)
                    {
                        clonedSynapses[i, j] = synapses[i, j];
                    }
                }
                Layer clonedLayer = new Layer(inputs, outputs, clonedSynapses);
                if(mutationRate > 0)
                   clonedLayer.Mutate(mutationRate);
                return clonedLayer;
            }

            //randomly mutates a synapse's weight.  Probability of any given synapse getting mutated = mutationRate/1000
            public void Mutate(int mutationRate)
            {
                System.Threading.Thread.Sleep(10);
                Random rand = new Random();
                int p;
                
                for (int i = 0; i < inputs; i++)
                {
                    for (int j = 0; j < outputs; j++)
                    {
                        p = rand.Next(0, 2000);
                        if (p <= mutationRate)
                        {
                            synapses[i, j] += ((float)rand.NextDouble() * maxDelta * 2) - maxDelta;
                            if (synapses[i, j] > maxWeight)
                                synapses[i, j] = maxWeight;
                            if (synapses[i, j] < -1 * maxWeight)
                                synapses[i, j] = -1 * maxWeight;
                        }
                    }
                }
            }

            //Takes in the input to this layer of the network as an array of floats and computes the output
            //Accumulates the product of each input times the synaptic weight, then performs sigmoid function
            public float[] ProcessInput(float[] input)
            {
                float[] outputBeforeSigmoid = new float[outputs];
                for (int i = 0; i < outputs; i++)
                    outputBeforeSigmoid[i] = 0;//initialize output to 0
                for(int from = 0; from < inputs; from++)
                {
                    for(int to = 0; to < outputs; to++)
                    {
                        outputBeforeSigmoid[to] += input[from] * synapses[from, to];
                    }
                }
                return Sigmoid(outputBeforeSigmoid);
            }

            //Takes in a vector of floats.  Returns a vector representing the sigmoid function of those numbers.
            //Sigmoid function used is f(x) = x/Sqrt(1+x^2)
            public float[] Sigmoid(float[] input)
            {
                float[] output = new float[outputs];
                for (int i = 0; i < outputs; i++)
                {
                    output[i] = input[i] / (float)Math.Sqrt(1 + input[i] * input[i]);
                }
                return output;
            }

            //creates string with every synaptic weight, one per line
            public override string ToString()
            {
                string reply = "";
                for (int i = 0; i < inputs; i++)
                    for (int j = 0; j < outputs; j++)
                        reply += (synapses[i, j] + Environment.NewLine);
                return reply;
            }
        }
    }
}
