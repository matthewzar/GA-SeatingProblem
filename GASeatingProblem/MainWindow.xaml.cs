using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
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
using GASeatingProblem.Seating;

namespace GASeatingProblem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string  TEST_FILE_NAME = @"F:\DEV\GA-SeatingProblem\Data_Example.xlsx";
        public MainWindow()
        {
            InitializeComponent();
        }

        private int MaxGenerations;
        private int PopulationSize;
        private int UpdateFrequency;
        
        private void LayoutPopulationThread(object threadNumber)
        {
            int threadNum = (int)threadNumber;

            var evaluator = new LayoutEvaluator();
            
            var allEmployees = new List<int>();
            for (var i = 0; i < evaluator.TotalEmployees; i++)
            {
                allEmployees.Add(i);
            }

            var pop = new LayoutPopulation();
            if(LayoutEvaluator.TargetLayout == null)
                pop.CreateRandomPopulation(allEmployees, PopulationSize);
            else
                pop.CreateHistoricallySimilarPopulation(allEmployees, PopulationSize);
            

            LayoutChromosome bestEver = null;
            double highestFitness = 0;
            for (int i = 0; i < MaxGenerations; i++)
            {
                pop.EvaluateCurrentPopulation(evaluator);

                if (pop.maxFitness > highestFitness)
                {
                    highestFitness = pop.maxFitness;
                    bestEver = pop.fittest;
                    if(threadNum == 0)
                        Console.WriteLine($"New best fitness: {highestFitness:F2}. {bestEver.conflictPercentage*100:F2}% in Conflict, {bestEver.teamSeatsUsedPercentage*100:F2}% in team seats, {bestEver.historicSeatingUsedPercentage*100:F2}% in previous seats");
                    else
                        Console.WriteLine($"New best fitness: {highestFitness:F2}. {bestEver.conflictPercentage * 100:F2}% in Conflict, {bestEver.teamSeatsUsedPercentage * 100:F2}% in team seats, {bestEver.historicSeatingUsedPercentage * 100:F2}% in previous seats. (Thread {threadNum})");
                }

                if (pop.maxFitness > LayoutEvaluator.GlobalBestFitness)
                {
                    LayoutEvaluator.GlobalBestFitness = pop.maxFitness;
                    LayoutEvaluator.GlobalBestChromosome = pop.fittest;
                }

                if (i % UpdateFrequency == 0)
                    Console.WriteLine($"{i}\t. Average fitness: {pop.averageFitness}, Max fitness: {pop.maxFitness}, Min Fitness: {pop.minFitness} {(threadNum==0?"": "Thread " + threadNum)}");

                pop = (LayoutPopulation)pop.GetNextGeneration();
            }
            pop.EvaluateCurrentPopulation(evaluator);
            if (LayoutEvaluator.GlobalBestFitness == highestFitness)
            {
//                Console.WriteLine(
//                    $"Max fitness: {pop.maxFitness}, Average fitness: {pop.averageFitness}, Min Fitness: {pop.minFitness}");
                Console.WriteLine($"Best Fitness Ever: {LayoutEvaluator.GlobalBestFitness}");

                LayoutEvaluator.DisplayLayoutFromChromosome(LayoutEvaluator.GlobalBestChromosome);
            }
        }

        private bool TryPromptForFileAndLoadData()
        {
            LayoutEvaluator.targetDate = date_to_calculate_for.SelectedDate ?? DateTime.Now;
            var evaluator = new LayoutEvaluator();

            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Title = "Select Daily Employee Roster";
            dlg.FileName = "employee_data.xlsx";
            dlg.DefaultExt = ".xlsx";
            dlg.Filter = "XLSX Files (*.xlsx)|*.xlsx";
            // Display OpenFileDialog by calling ShowDialog method 
            var result = dlg.ShowDialog();
            // Get the selected file name and display in a TextBox 
            if (result != true)
            {
                return false;
            }
            evaluator.InitialiseFromFile(dlg.FileName);
            return true;
        }

        private bool TryPromptForPriorLayout()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Title = "Select Target Historic Layout";
            dlg.FileName = "prior_layout.json";
            dlg.DefaultExt = ".json";
            dlg.Filter = "JSON Files (*.json)|*.json";
            // Display OpenFileDialog by calling ShowDialog method 
            var result = dlg.ShowDialog();
            // Get the selected file name and display in a TextBox 
            if (result != true)
            {
                return false;
            }

            previousDaysLayoutFile = dlg.FileName;
            LayoutEvaluator.LoadPriorSeating(previousDaysLayoutFile);
            return true;
        }
        
        private void btn_StartCalculating(object sender, RoutedEventArgs e)
        {
            if (!TryPromptForFileAndLoadData())
            {
                MessageBox.Show("No file selected");
                return;
            }

            if (cb_UseHistoricSeats.IsChecked == true  && !TryPromptForPriorLayout())
            {
                MessageBox.Show("No prior target layout selected.");
                return;
            }

            btn_start.IsEnabled = false;
            MaxGenerations = int.Parse(tb_max_generations.Text);
            PopulationSize = int.Parse(tb_pop_size.Text);
            UpdateFrequency = int.Parse(tb_update_freq.Text);
            LayoutChromosome.MIN_SWAP_MUTATIONS = int.Parse(tb_min_swap.Text);
            LayoutChromosome.MAX_SWAP_MUTATIONS = int.Parse(tb_min_swap.Text);
            LayoutChromosome.CONLICT_SHIFT_RANGE = int.Parse(tb_min_swap.Text);

            tb_pop_size.IsEnabled = false;
            tb_max_swap.IsEnabled = false;
            tb_conflict_shift.IsEnabled = false;
            tb_min_swap.IsEnabled = false;
            
            for (int i = 1; i <= 8; i++)
            {
                Thread newThread = new Thread(LayoutPopulationThread);
                newThread.Start(i);
                
                Thread.Sleep(100);
            }

            btn_SaveBest.IsEnabled = true;
        }


        private void btn_SaveBest_Click(object sender, RoutedEventArgs e)
        {
            LayoutEvaluator.SaveForFuturePriorSeating("prior_layout.json");
        }

        private void tb_max_generations_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(tb_max_generations?.Text))
                int.TryParse(tb_max_generations?.Text, out MaxGenerations);
            if (!string.IsNullOrWhiteSpace(tb_pop_size?.Text))
                int.TryParse(tb_pop_size?.Text, out PopulationSize);
            if (!string.IsNullOrWhiteSpace(tb_update_freq?.Text))
                int.TryParse(tb_update_freq?.Text, out UpdateFrequency);
        }

        private string previousDaysLayoutFile = null;


        //This block of unused methods contains legacy code from testing and devolopment.
        //It can used for examples in some case, but don't expect it to work.
        #region OLD CODE
        private void button2_Click(object sender, RoutedEventArgs e)
        {
            var evaluator = new LayoutEvaluator();
            evaluator.InitialiseFromFile(TEST_FILE_NAME);
            LayoutPopulationThread(0);
        }
        
        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LayoutChromosome.crossoverType = (CrossoverType)comboBox.SelectedItem;
        }

        private void btn_exploration_Click(object sender, RoutedEventArgs e)
        {
            //Come back to this general GA-algorithm once some concrete examples have been created.
            return;

            //Create an evaluator. For the seating problem (SP) it will be one that scores based on a DB of known people
            ILayoutEvaluator<int> evaluator = null;
            evaluator.InitialiseFromFile("MyTestFile.txt");

            //TODO, change this declartion to a loop so that many chromosomes can be added to the population.
            IPopulation<int> pop = null;
            IChromosome<int> chromosome = null;
            chromosome.InitialiseFromStrings(new List<string> { "Person 1" });
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
                    //                    if (chrome.GetFitness() > currentMaxFitness)
                    //                    {
                    //                        currentMaxFitness = chrome.GetFitness();
                    //                    }
                }

                pop = pop.GetNextGeneration();
            }

            Console.WriteLine($"Got to a fitness of {currentMaxFitness}, in {currentGeneration} generations");
            //TODO - store the fittest chromosome.
            Console.WriteLine("DONE");
        }

        private void btn_testDesk_Click(object sender, RoutedEventArgs e)
        {
            for (int row = 30; row >= 1; row--)
            {
                var desk = new Desk(row, 1);
                Console.WriteLine($"Row {row}, col {1}, index: {desk.GetIndex()}");
            }
            for (int row = 77; row >= 60; row--)
            {
                var desk = new Desk(row, 1);
                Console.WriteLine($"Row {row}, col {1}, index: {desk.GetIndex()}");
            }
        }

        private void btn_testDeskFromIndex_Click(object sender, RoutedEventArgs e)
        {
            var toDisplay = new StringBuilder();

            var temp = new LayoutEvaluator();
            temp.InitialiseFromFile(TEST_FILE_NAME);

            Console.WriteLine("#### Generate raw linear layout from indexes, and demo desk reversibility.");
            var rawLayout = new List<string>();
            for (int i = 0; i < 146; i++)
            {
                rawLayout.Add($"{i}");
                Desk desk = Desk.ConvertIndexToDesk(i);
                toDisplay.AppendLine($"From Index {i} got: Row {desk.Row}, col {desk.Col}, index {desk.GetIndex()}");
                //Console.WriteLine($"From Index {i} got: Row {desk.Row}, col {desk.Col}, index {desk.GetIndex()}");

                if (desk.GetIndex() != i) throw new Exception($"From Index {i} got: Row {desk.Row}, col {desk.Col}, index {desk.GetIndex()}");
            }
            Console.WriteLine(toDisplay.ToString());
            Console.WriteLine("\n\n");

            Console.WriteLine("#### Personel Conflicts:");
            LayoutEvaluator.DisplayEmployeeConflicts();
            Console.WriteLine("\n\n");

            Console.WriteLine("#### Create LayoutChromosome from raw layout, then show desk layout for said chromosome:");
            LayoutChromosome layout = new LayoutChromosome();
            layout.InitialiseFromStrings(rawLayout);
            LayoutEvaluator.DisplayLayoutFromChromosome(layout);

            Console.WriteLine("\n\n");
            Console.WriteLine("#### Mutate the above layout, and show the new version: ");
            layout.Mutate();
            LayoutEvaluator.DisplayLayoutFromChromosome(layout);

        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            var temp = new LayoutEvaluator();
            temp.InitialiseFromFile(TEST_FILE_NAME);

            var allEmployees = new List<int>();
            for (var i = 0; i < temp.TotalEmployees; i++)
            {
                allEmployees.Add(i);
            }

            LayoutChromosome layout1 = new LayoutChromosome();
            LayoutChromosome layout2 = new LayoutChromosome();
            layout1.InitialiseFromRawInputs(allEmployees.GetRange(0, 146));
            GlobalRandom.YatesShuffle(allEmployees);
            layout2.InitialiseFromRawInputs(allEmployees.GetRange(0, 146));

            Console.WriteLine("#### Personel Conflicts:");
            LayoutEvaluator.DisplayEmployeeConflicts();

            Console.WriteLine("#### Group 1");
            LayoutEvaluator.DisplayLayoutFromChromosome(layout1);
            Console.WriteLine("#### Group 2");
            LayoutEvaluator.DisplayLayoutFromChromosome(layout2);

            var offspring = layout1.CrossoverWith(layout2);
            Console.WriteLine("\n#### Child 1");
            LayoutEvaluator.DisplayLayoutFromChromosome((LayoutChromosome)offspring.Item1);
            Console.WriteLine("#### Child 2");
            LayoutEvaluator.DisplayLayoutFromChromosome((LayoutChromosome)offspring.Item2);
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            var evaluator = new LayoutEvaluator();
            evaluator.InitialiseFromFile(TEST_FILE_NAME);

            var allEmployees = new List<int>();
            for (var i = 0; i < evaluator.TotalEmployees; i++)
            {
                allEmployees.Add(i);
            }

            var layout1 = new LayoutChromosome();
            var layout2 = new LayoutChromosome();
            layout1.InitialiseFromRawInputs(allEmployees.GetRange(0, 146));
            GlobalRandom.YatesShuffle(allEmployees);
            layout2.InitialiseFromRawInputs(allEmployees.GetRange(0, 146));

            Console.WriteLine("#### Group 1");
            LayoutEvaluator.DisplayLayoutFromChromosome(layout1);
            Console.WriteLine("#### Group 2");
            LayoutEvaluator.DisplayLayoutFromChromosome(layout2);


            var pop = new LayoutPopulation();
            pop.AddChromosome(layout1);
            pop.AddChromosome(layout2);
            pop.EvaluateCurrentPopulation(evaluator);
            pop = (LayoutPopulation)pop.GetNextGeneration();

            var cnt = 1;
            foreach (var child in pop.EnumerateChromosomes())
            {
                Console.WriteLine("\n#### Child {0}", cnt++);
                LayoutEvaluator.DisplayLayoutFromChromosome((LayoutChromosome)child);
            }
        }
        #endregion

    }
}
