using System;
using System.Collections.Generic;

namespace GASeatingProblem.Seating
{
    public class Person
    {
        public string Name { get; private set; }
        public int Index { get; private set; }
        public int Team { get; private set; }
        
        public List<Person> Conflicts;

        public Person(string name, int index, int team)
        {
            Name = name;
            Index = index;
            Team = team;
            Conflicts = new List<Person>();
        }

        public bool TryAddConflict(Person newConflict)
        {
            if (Index == newConflict.Index) 
                throw new ArgumentException($"Person {Name} (ID {Index}), is being set to conflict with themself.");
            
            foreach (var conflict in Conflicts)
            {
                if(conflict.Index == newConflict.Index)
                    return false;
            }
            
            //By making conflict bi-directional, we don't have to backtrack on each person in a layout to find conflicts.
            //Instead (for example) having person X in desk 10, and needing to look from desk 5 to 15 to look for their conflictors,
            //You can just look at 11 to 15, because any conflictors in 5 to 9 will have already found person X (marking them both)
            Conflicts.Add(newConflict);
            newConflict.TryAddConflict(this);
            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(Person)) return false;

            return ((Person) obj).Index == Index &&
                   ((Person) obj).Name == Name &&
                   ((Person) obj).Team == Team;
        }
    }
}