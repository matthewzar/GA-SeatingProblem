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
            ILayoutEvaluator<int> evaluator = null;
            evaluator.InitialiseFromFile("MyTestFile.txt");

            //TODO, change this declartion to a loop so that many chromosomes can be added to the population.
            IPopulation<int> pop = null;
            IChromosome<int> chromosome = null;
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
