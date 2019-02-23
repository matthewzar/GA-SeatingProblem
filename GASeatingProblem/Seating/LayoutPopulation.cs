using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Animation;

namespace GASeatingProblem.Seating
{
    public class LayoutPopulation : IPopulation<int>
    {
        public Random RNG = GlobalRandom.GetLocalRNG();

        private List<IChromosome<int>> layouts = new List<IChromosome<int>>();
        public LayoutChromosome fittest;
        public double maxFitness = double.MinValue, minFitness = double.MaxValue;
        public double averageFitness, totalFitness;
        private int totalChromosomes;

        public void AddChromosome(IChromosome<int> chromosome)
        {
            layouts.Add(chromosome);
        }

        public IPopulation<int> GetNextGeneration()
        {
            //TODO - add a legality check before adding offspring. Right now an illegal child will probably just get a fitness of 0, and therefore become unselectable.
            int legalOffspringAdded = 0;
            var newPopulation = new LayoutPopulation();

            while (legalOffspringAdded < layouts.Count)
            {
                var parent1 = GetWeightedOffspring();
                var parent2 = GetWeightedOffspring();

                //No breeding with yourself
                if(parent1==parent2)
                    continue;

                var offspring = parent1.CrossoverWith(parent2);
                //This is where legality of offspring might in the future be checked, and 1 or both children rejected.

                ((LayoutChromosome)offspring.Item1).SwapOutDuplicates();
                ((LayoutChromosome)offspring.Item2).SwapOutDuplicates();
                
                offspring.Item1.Mutate();
                offspring.Item2.Mutate();

                newPopulation.AddChromosome(offspring.Item1);
                newPopulation.AddChromosome(offspring.Item2);

                legalOffspringAdded += 2;
            }

            return newPopulation;
        }

        public void CreateRandomPopulation(List<int> allEmployees, int populationSize)
        {
            Random RNG = GlobalRandom.GetLocalRNG();
            for (int i = 0; i < populationSize; i++)
            {
                GlobalRandom.YatesShuffle(allEmployees, RNG);
                var layout = new LayoutChromosome();
                layout.InitialiseFromRawInputs(allEmployees.GetRange(0, 146));
                AddChromosome(layout);
            }
        }

        public void CreateHistoricallySimilarPopulation(List<int> allEmployees, int populationSize)
        {
            Random RNG = GlobalRandom.GetLocalRNG();

            //Get a list of employees that need to be added into open spaces from yesterday
            var unnacountedForEmployees = new List<int>(); //Yes, this list can contain empty desks, but only if allEmployees required padding
            var rawLayout = LayoutEvaluator.TargetLayout.GetRawElements();
            foreach (var presentEmployee in allEmployees)
            {
                if(!rawLayout.Contains(presentEmployee))
                    unnacountedForEmployees.Add(presentEmployee);
            }
            
            for (int i = 0; i < populationSize; i++)
            {
                //Shuffle the employees to be added
                GlobalRandom.YatesShuffle(unnacountedForEmployees, RNG);
                
                //Clone the target layout
                var filledInHistoricLayout = new List<int>(LayoutEvaluator.TargetLayout.GetRawElements());

                //Move over the partial layout, and put unnavounted-for employees into empty spaces
                var newPeopleIndex = 0;
                for (int j = 0; j < filledInHistoricLayout.Count; j++)
                {
                    //If someone historic has been assigned... don't assign them a new value
                    if(filledInHistoricLayout[j] != -1) continue;
                    filledInHistoricLayout[j] = unnacountedForEmployees[newPeopleIndex++];
                }
                
                var layout = new LayoutChromosome();
                layout.InitialiseFromRawInputs(filledInHistoricLayout);
                AddChromosome(layout);
            }
        }

        private IChromosome<int> GetWeightedOffspring()
        {
            //This computes a number from 0 to totalFitness, which will be gradually decreased to find whether current layout was the 'tipping point'
            //This works, becuase highly fit chromosomes are more likely to push over the boundary, and are therefore more likely to be picked.
            var fitnessBound = RNG.NextDouble() * totalFitness;

            foreach (var layout in layouts)
            {
                fitnessBound -= layout.GetFitness();
                if (fitnessBound <= 0)
                {
                    return layout;
                }
            }

            //Should never get here, but just in case, return the final chromosome (even in the case of all fitness being zero)
            return layouts[layouts.Count - 1];
        }

        public IEnumerable<IChromosome<int>> EnumerateChromosomes()
        {
            foreach (var layout in layouts)
            {
                yield return layout;
            }
        }

        public void EvaluateCurrentPopulation(ILayoutEvaluator<int> evaluator)
        {
            totalChromosomes = layouts.Count;
            totalFitness = 0;

            foreach (var layout in layouts)
            {
                double fitness = evaluator.GetFitness(layout);
                layout.SetFitness(fitness);
                totalFitness += fitness;
                if (fitness > maxFitness)
                {
                    maxFitness = fitness;
                    fittest = (LayoutChromosome)layout;
                }
                minFitness = Math.Min(fitness, minFitness);
            }

            averageFitness = totalFitness / totalChromosomes;
        }
    }
}