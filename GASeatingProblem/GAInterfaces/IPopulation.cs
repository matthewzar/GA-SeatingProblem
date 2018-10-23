using System.Collections.Generic;

namespace GASeatingProblem
{
    interface IPopulation<T>
    {
        /// <summary>
        /// Add a new chromosome to the current population.
        /// </summary>
        /// <param name="chromosome"></param>
        void AddChromosome(IChromosome<T> chromosome);

        /// <summary>
        /// Using fitnesses stored from EvaluateCurrentPopulation(), perform crossovers, mutations, and offspring rejections.
        /// </summary>
        /// <param name="evaluator"></param>
        /// <returns>A new generations population of chromosomes</returns>
        IPopulation<T> GetNextGeneration();

        IEnumerable<IChromosome<T>> EnumerateChromosomes();
        
        void EvaluateCurrentPopulation(ILayoutEvaluator<T> evaluator);
    }
}