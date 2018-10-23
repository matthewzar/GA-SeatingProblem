using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GASeatingProblem.Seating
{
    class LayoutChromosome : IChromosome<int>
    {
        readonly List<int> personIDs = new List<int>();
        
        public void InitialiseFromStrings(List<string> rawStrings)
        {
            //Each person gets a unique ID (corresponding to a lookup-array somewhere else).
            personIDs.Clear();
            foreach (var id in rawStrings)
            {
                personIDs.Add(int.Parse(id));
            }
        }

        public Tuple<IChromosome<int>, IChromosome<int>> CrossoverWith(IChromosome<int> partner)
        {
            //TODO - take current class, and partner class, and make real offspring.
            var child1 = new LayoutChromosome();
            var child2 = new LayoutChromosome();

            return new Tuple<IChromosome<int>, IChromosome<int>>(child1, child2);
        }
        
        public void Mutate()
        {
            //TODO - change this to mutate more randomly, AND mutate in a way that can bring in people who have been excluded.

            //A simple swap like this ensure that no duplication occurs.
            var temp = personIDs[0];
            personIDs[0] = personIDs[1];
            personIDs[1] = temp;
        }

        public List<int> GetRawElements()
        {
            return personIDs;
        }
    }
}
