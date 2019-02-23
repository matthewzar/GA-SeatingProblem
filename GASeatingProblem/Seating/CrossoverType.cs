using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GASeatingProblem.Seating
{
    public enum CrossoverType
    {
        LeftRight = 0,     // [1, 2, 3, 4], [5, 6, 7, 8] --> [1, 6, 7, 8], [5, 2, 3, 4]
        CentreChunk = 1,   // [1, 2, 3, 4], [5, 6, 7, 8] --> [1, 6, 7, 4], [5, 2, 3, 8]
        RandomMerge = 2,   // [1, 2, 3, 4], [5, 6, 7, 8] --> [1, 6, 3, 8], [5, 2, 8, 4]
        Clone = 3          // [1, 2, 3, 4], [5, 6, 7, 8] --> [1, 2, 3, 4], [5, 6, 7, 8]
    }
}
