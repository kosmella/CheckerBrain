# CheckerBrain
The idea for this program came from the book Blondie24 by David B. Fogel.  He created a checkers-playing AI that destroyed the competition on MSN games.

His program used a neural network that was trained by an evolutionary algorithm.  The neural network was used as an evaluation function to evaluate a minimax tree and determine the next move to take.  It would take the max allowable time (2 minutes) to evalute deeply in the tree as possible.

Ever since reading the book, I wondered how good a neural network could get at checkers without using the minimax algorithm to look ahead several moves.  I wondered if a sufficiently complex network could still make decent choices without explicitly looking ahead.  This was my attempt to find out.

This program is also serving as a learning platform for me.  I'm learning C# and currently taking a course on XAML.  This is very much a work in progress.  My future plan is to refactor this program to follow the MVVM pattern as I learn it.

I also have some plans to tweak the learning algorithm to avoid getting stuck at a local maximum and to improve performance.  First, I would like to try representing synaptic weights as integers instead of floating point #s.  FP precision is overkill.  Also, I have some tweaks in mind in order to maintain a diverse list of CheckerBrains to train against.  Currently, the training bracket quickly fills with a bunch of close relatives, which doesn't provide a diverse enough range of strategies to train against.
