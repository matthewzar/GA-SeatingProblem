using System;
using System.Collections.Generic;

namespace GASeatingProblem
{
    /// <summary>
    /// 1D chromosome. Basically a wrapper around a List of T - where T might be strings (to perform a lookup),
    /// bools (to represent a binary number), doubles to represent individual argument inputs, etc...
    ///
    /// To a certain extent the Type (T) will determine how mutations and crossovers occur.
    /// For example, if T==String/Key the changes will have to ensure that the same value doesn't occur twice.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    interface IChromosome<T>
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
        Tuple<IChromosome<T>, IChromosome<T>> CrossoverWith(IChromosome<T> partner);

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

        //we might want to add these, simply so that IPopulation doesn't have to track fitnesses seperately.
//        void SetFitness(double fitness);
//        double GetFitness();

        List<T> GetRawElements();
    }
}