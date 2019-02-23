using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace GASeatingProblem.Seating
{
    public class LayoutChromosome : IChromosome<int>
    {
        public Random RNG = GlobalRandom.GetLocalRNG();
        public static int MIN_SWAP_MUTATIONS = 1;
        public static int MAX_SWAP_MUTATIONS = 3;
        public static int CONLICT_SHIFT_RANGE = 30;
        public static CrossoverType crossoverType = CrossoverType.Clone;
        
        /// <summary>
        /// A list of person IDs, in the order that they will be assigned to desks.
        /// For example, [3,2,5,190 ...], will have person 190 at desk index 3 (which corresponds to Row 30, Col 4)
        /// </summary>
        private readonly List<int> personIDs = new List<int>();
        
        //The fitness of this chromosome. Negative values mean unset, 0 means probably illegal or REALLY bad, numbers above zero mean a viable layout whose quality scales with the size of the value.
        private double fitness = -1;
        public double conflictPercentage = -1;
        public double teamSeatsUsedPercentage = -1;
        public double historicSeatingUsedPercentage = -1;


        public void InitialiseFromStrings(List<string> rawStrings)
        {
            //Each person gets a unique ID (corresponding to a lookup-array somewhere else).
            personIDs.Clear();
            foreach (var id in rawStrings)
            {
                personIDs.Add(int.Parse(id));
            }
        }

        public void InitialiseFromRawInputs(List<int> rawInputs)
        {
            personIDs.Clear();
            foreach (var id in rawInputs)
            {
                personIDs.Add(id);
            }
        }

        public Tuple<IChromosome<int>, IChromosome<int>> CrossoverWith(IChromosome<int> partner)
        {
            var parent1LayoutData = this.GetRawElements();
            var parent2LayoutData = partner.GetRawElements();

            var newRawData1 = new List<int>();
            var newRawData2 = new List<int>();

            //Select the condition to use when checking which parent should contribute to which child
            Func<int, bool> swapCondition;
            switch (crossoverType)// RNG.Next(3))
            {
                case CrossoverType.LeftRight:
                    var middle = RNG.Next(1,parent1LayoutData.Count-1); //Pick an integer (other than 0 and 'end') to use as the mid-point
                    swapCondition = idx => idx >= middle;
                    break;
                case CrossoverType.CentreChunk:
                    var first = RNG.Next(1, parent1LayoutData.Count - 2); 
                    var second = RNG.Next(first+1, parent1LayoutData.Count - 1);
                    swapCondition = idx => idx >= first && idx < second;
                    break;
                case CrossoverType.RandomMerge:
                    swapCondition = idx => RNG.Next(2) == 0; 
                    break;
                case CrossoverType.Clone:
                    swapCondition = idx => true;
                    break;
                default:
                    swapCondition = idx => true;
                    break;
            }

            for (var i = 0; i < parent1LayoutData.Count; i++)
            {
                if (swapCondition(i))
                {
                    newRawData1.Add(parent2LayoutData[i]);
                    newRawData2.Add(parent1LayoutData[i]);
                }
                else
                {
                    newRawData1.Add(parent1LayoutData[i]);
                    newRawData2.Add(parent2LayoutData[i]);
                }
            }

            var child1 = new LayoutChromosome();
            var child2 = new LayoutChromosome();
            
            child1.InitialiseFromRawInputs(newRawData1);
            child2.InitialiseFromRawInputs(newRawData2);

            return new Tuple<IChromosome<int>, IChromosome<int>>(child1, child2);
        }
        
        public void Mutate()
        {
            //TODO - change this to also mutate in a way that can bring in people who have been excluded (potentially replacing existing people).
            
            var maxSwaps = RNG.Next(MIN_SWAP_MUTATIONS, MAX_SWAP_MUTATIONS + 1); //add 1 to make upper-bound inclusive.

            //Simple swaps (rather than remove, and pick from a list of availables) ensures that no duplication occurs.
            
            //A complete reshuffle of the whole layout, rare, but could add valuable diversity
            if (RNG.NextDouble() < 0.001) //0.1% chance per mutating chromosome
            {
                GlobalRandom.YatesShuffle(personIDs, RNG);
                //Perform this check and shuffle first, as any prior work will be lost otherwise. 
            }

            //Perform a few any-range swaps.
            for (var i = 0; i < maxSwaps; i++)
            {
                var swapIdx1 = RNG.Next(personIDs.Count);
                var swapIdx2 = RNG.Next(personIDs.Count);

                var temp = personIDs[swapIdx1];
                personIDs[swapIdx1] = personIDs[swapIdx2];
                personIDs[swapIdx2] = temp;
            }

            //Perform many limited range swaps
            bool performMiniShuffle = RNG.Next() % 10 == 0; //~10% of mutations will include an "everyone stand up, and trade seats with a nearby neighbor" shuffle.
            var conflicts = LayoutEvaluator.CountInRangeConflicts(this);
            for (int i = 0; i < conflicts.Length; i++)
            {
                if (conflicts[i] > 1)
                {
                    //choose an index within 12 steps on either side of the current index
                    var swapIdx = RNG.Next(Math.Max(0, i- CONLICT_SHIFT_RANGE), 
                                           Math.Min(i+ CONLICT_SHIFT_RANGE, conflicts.Length));
                    
                    var temp = personIDs[i];
                    personIDs[i] = personIDs[swapIdx];
                    personIDs[swapIdx] = temp;
                }
                else if (performMiniShuffle)
                {
                    //Shifting up to 2 spaces left/right will (more often than not), leave you in the same row.
                    //But it will allow for subtle movement over time, where non-conflict people can move towards teams.
                    var swapIdx = RNG.Next(Math.Max(0, i - 1),
                        Math.Min(i + 2, conflicts.Length));

                    var temp = personIDs[i];
                    personIDs[i] = personIDs[swapIdx];
                    personIDs[swapIdx] = temp;
                }
            }

            
        }

        /// <summary>
        /// Given an array of how many 
        /// </summary>
        /// <param name="occurences"></param>
        public void SwapOutDuplicates()
        {
            var occurences = LayoutEvaluator.CountEmployeeOccurences(this);
            var unusedEmployees = new List<int>();
            for (var i = 0; i < occurences.Length; i++)
            {
                if(occurences[i] == 0)
                    unusedEmployees.Add(i);
            }
            //Have to shuffle them, otherwise a bias occurs where the first unused emplyees are far more likely to be placed
            //near the beginning of the layout (decreasing variability).
            GlobalRandom.YatesShuffle(unusedEmployees, RNG);

            int unusedIndex = 0;
            for (int i = 0; i < personIDs.Count; i++)
            {
                var currentPersonIndex = personIDs[i];
                //If the person has a legal number of desks, then don't do anything to them
                if(occurences[currentPersonIndex] <= 1) continue;

                var newID = unusedEmployees[unusedIndex++];
                //Replace the person in the current layout at desk 'i' with the person we know has no desk
                personIDs[i] = newID;
                //Update occurence counters so that duplicate counterparts that have now been removed, aren't removed again.
                occurences[newID]++;
                occurences[currentPersonIndex]--;
            }
        }

        public void SetFitness(double newFitness)
        {
            if(newFitness < 0) throw new ArgumentException($"Can't set a negative fitness ({newFitness})");

            fitness = newFitness;
        }

        public double GetFitness()
        {
            if (fitness < 0) throw new Exception("Found a chromosome whose fitness was not set, before that fitness was requested.");

            return fitness;
        }

        public List<int> GetRawElements()
        {
            return personIDs;
        }
    }
}
