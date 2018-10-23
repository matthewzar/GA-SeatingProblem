namespace GASeatingProblem
{
    interface ILayoutEvaluator<T>
    {
        /// <summary>
        /// Get the numeric fitness (or fitnesses) of a single chromosome.
        /// A simple evaluator (such as finding the maximum of an equation) might only return 1 number (the output of the equation for the chromosomes input values).
        /// A more complex evaluator (such as seating allocation) might return: total rejected individuals, total vacant seats, total dispossesed "permanent" seats, people far from teams.
        /// Optionally the more complex case could internally merge those values into a single score.
        /// </summary>
        /// <param name="chromosome"></param>
        /// <returns></returns>
        double GetFitness(IChromosome<T> chromosome);
        
        /// <summary>
        /// A means of testing if a chromosome is legal. Good for rejecting offspring that might break a system. 
        /// </summary>
        /// <returns></returns>
        bool IsValid(IChromosome<T> chromosome);

        /// <summary>
        /// Populate any databases with the required info. For example, a Person-Rule lookup table, a Desk-Coordinate matrix,
        /// A Person-Conflict DB etc.
        /// </summary>
        /// <param name="fileName"></param>
        void InitialiseFromFile(string fileName);
        //TODO - change this signature to accept something like an IFileParser, which would have a method like: "Enumerable<string> ParseFile(string address)"

        //NOTE - for the DeskLayoutEvaluator, it may be cheaper to store known arrangements (and their fitnesses) that to recompute them. A simple Chrome.ToString + Dictionary<string, fitness>.
    }
}