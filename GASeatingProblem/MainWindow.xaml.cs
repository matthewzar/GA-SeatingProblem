using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace GASeatingProblem
{
    interface IChromosome
    {
        /// <summary>
        /// Populate the chromosomes internal structure based on a list of string.
        /// For example ["John Smith", "Jane Doe", "Mr Gates"], would create a chromosome where Jane Doe is assigned to Desk 2 (index 1).
        /// </summary>
        /// <param name="rawStrings"></param>
        void InitialiseFromStrings(List<string> rawStrings);

        /// <summary>
        /// Create 2 offspring from the current chromosome and the provided parter.
        /// The child chromosome are only guaranteed to follow the correct structure, they
        /// may not actually be valid.
        /// For example CrossoverWith(["John Smith", "Jane Doe", "Mr Gates"], ["Mr Gates", "John Smith", "Jane Doe"]);
        /// May return a child of ["Mr Gates", "John Smith", "Mr Gates"] -- valid items at each position, but the same item may not be allowed twice.
        ///
        /// Doesn't either parent.
        /// </summary>
        /// <param name="partner"></param>
        /// <returns></returns>
        Tuple<IChromosome, IChromosome> CrossoverWith(IChromosome partner);

        /// <summary>
        /// Asexual self-modification. Typically a weighted random number of elements would be changed to random new values.
        /// For something where all items can only occur once, mutation might involve swapping 2 items, or removing one only
        /// to replace it with one that isn't is use yet.
        /// For example ["Mr Gates", "John Smith", "Irritating Isaac"] might mutate to ["Mr Gates", "John Smith", "Amicable Anne"]
        ///
        /// Changes the chromosome permanently, so it should only be done on clones.
        /// </summary>
        /// <returns></returns>
        void Mutate();

        /// <summary>
        /// A means of testing if a chromosome is legal. Good for rejecting offspring that might break a system. 
        /// </summary>
        /// <returns></returns>
        bool IsValid();
    }

    interface IPopulation
    {
        /// <summary>
        /// Add a new chromosome to the current population.
        /// </summary>
        /// <param name="chromosome"></param>
        void AddChromosome(IChromosome chromosome);

        /// <summary>
        /// Using fitnesses stored from EvaluateCurrentPopulation(), perform crossovers, mutations, and offspring rejections.
        /// </summary>
        /// <param name="evaluator"></param>
        /// <returns>A new generations population of chromosomes</returns>
        IPopulation GetNextGeneration();

        IEnumerable<IChromosome> EnumerateChromosomes();
        
        void EvaluateCurrentPopulation(ILayoutEvaluator evaluator);
    }

    interface ILayoutEvaluator
    {
        /// <summary>
        /// Get the numeric fitness (or fitnesses) of a single chromosome.
        /// A simple evaluator (such as finding the maximum of an equation) might only return 1 number (the output of the equation for the chromosomes input values).
        /// A more complex evaluator (such as seating allocation) might return: total rejected individuals, total vacant seats, total dispossesed "permanent" seats, people far from teams.
        /// Optionally the more complex case could internally merge those values into a single score.
        /// </summary>
        /// <param name="chromosome"></param>
        /// <returns></returns>
        double GetFitness(IChromosome chromosome);

        /// <summary>
        /// Populate any databases with the required info. For example, a Person-Rule lookup table, a Desk-Coordinate matrix,
        /// A Person-Conflict DB etc.
        /// </summary>
        /// <param name="fileName"></param>
        void InitialiseFromFile(string fileName);
        //TODO - change this signature to accept something like an IFileParser, which would have a method like: "Enumerable<string> ParseFile(string address)"

        //NOTE - for the DeskLayoutEvaluator, it may be cheaper to store known arrangements (and their fitnesses) that to recompute them. A simple Chrome.ToString + Dictionary<string, fitness>.
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btn_exploration_Click(object sender, RoutedEventArgs e)
        {
            //Create an evaluator. For the seating problem (SP) it will be one that scores based on a DB of known people
            ILayoutEvaluator evaluator = null;
            evaluator.InitialiseFromFile("MyTestFile.txt");

            //TODO, change this declartion to a loop so that many chromosomes can be added to the population.
            IPopulation pop = null;
            IChromosome chromosome = null;
            chromosome.InitialiseFromStrings(new List<string>{"Person 1"});
            pop.AddChromosome(chromosome);
            
            double targetFitness = 100;
            double currentMaxFitness = double.MinValue;
            int maxGenerations = 1000;
            int currentGeneration = 0;

            //Keep looping until either the max generations are exeeded, or the current max fitness surpasses the target
            while (currentGeneration++ < maxGenerations && currentMaxFitness < targetFitness)
            {
                pop.EvaluateCurrentPopulation(evaluator);

                //Get the highest fitness. Probably move this logic into the IPopulation interface.
                foreach (var chrome in pop.EnumerateChromosomes())
                {
                    if (chrome.GetFitness() > currentMaxFitness)
                    {
                        currentMaxFitness = chrome.GetFitness();
                    }
                }

                pop = pop.GetNextGeneration();
            }

            Console.WriteLine($"Got to a fitness of {currentMaxFitness}, in {currentGeneration} generations");
            //TODO - store the fittest the chromosome.
            Console.WriteLine("DONE");
        }
    }
}
