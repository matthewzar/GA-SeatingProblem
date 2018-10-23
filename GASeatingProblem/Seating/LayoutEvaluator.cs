using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GASeatingProblem.Seating
{
    class LayoutEvaluator : ILayoutEvaluator<int>
    {
        public double GetFitness(IChromosome<int> chromosome)
        {
            throw new NotImplementedException();
        }

        public bool IsValid(IChromosome<int> chromosome)
        {
            throw new NotImplementedException();
        }

        public void InitialiseFromFile(string fileName)
        {
            throw new NotImplementedException();
        }
    }
}
