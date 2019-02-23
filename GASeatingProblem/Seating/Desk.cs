using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GASeatingProblem.Seating
{
    public class Desk
    {
        private int index = -1;
        public readonly int Row, Col;
        public int TeamNumber;

        public Desk(int row, int col)
        {
            Row = row;
            Col = col;
            GetIndex();
        }

        public int GetIndex()
        {
            //ensure that the index is computed once, to avoid expensive recalculations;
            if (index != -1) return index;

            if (Row <= 30 && Row >= 1)
            {
                return GetWestSideIndex();
            }

            if (Row <= 77 && Row >= 60)
            {
                return GetEastSideIndex();
            }

            throw new InvalidDataException(
                $"Values for row are bound between 1 to 30 and 60 to 77 (inclusive). A value of {Row} was provided.");
        }

        public static int GetDividersBetween(Desk firstDesk, Desk SecondDesk)
        {
            return 0;
        }

        private static readonly int[] westSideColCounts = new int[]
        {
            4, 4, //29, 30
            5, 5, 5, 5, 5, 5, //23 - 28
            3, 3, 3, 3, 3, 3, 3, 3, //15-22
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, //3-14
            3, 3 //1, 2
        };

        public static Desk ConvertIndexToDesk(int targetIndex)
        {
            int current = 0;
            for (int i = 0; i < westSideColCounts.Length; i++)
            {
                current += westSideColCounts[i];

                if (current > targetIndex)
                {
                    return new Desk(30-i, westSideColCounts[i] - (current - targetIndex)  + 1);
                }
            }

            for (int row = 77; row >= 60; row--)
            {
                current += 3;

                if (current > targetIndex)
                {
                    return new Desk(row, 3 - (current - targetIndex) + 1);
                }
            }

            return null;
        }
        
        private int GetWestSideIndex()
        {
            int idx = 0;
            for (int i = 0; i < westSideColCounts.Length; i++)
            {
                if (30 - i == Row)
                {
                    index = idx + Col - 1;
                    return index; //subtract 1, because the Column are numbered from 1 (rather than 0).
                }

                idx += westSideColCounts[i];
            }

            throw new Exception($"The west side has 30 rows (1 to 30 inclusive). But a desk of row {Row} was found");
        }
        private int GetEastSideIndex()
        {
            //Get the final index of the west side, as a starting point
            int idx = westSideColCounts.Sum();

            //TODO - somehow condense this into a single equation. Something like "return (77-Row)*3+Col
            for (int row = 77; row >= 60; row--)
            {
                if (row == Row)
                {
                    index = idx + Col - 1;
                    return index;
                }
                idx += 3;
            }
            throw new Exception($"The east side has {77-60} rows (60 - 77 inclusive). But a desk of row {Row} was found");
        }

        public int countSeperatorsBetweenDesks(int otherRow)
        {
            int first;
            int second;
            if (this.Row < 30)
            {
                if (otherRow < 30)
                {
                    //Can't just use abs(), as the modulous operator 
                    first = Math.Max(Row, otherRow);
                    second = Math.Min(Row, otherRow);
                    if(first % 2 == 0)
                        return (first-second-2) / 2;
                    return (first - second - 1) / 2;
                }

                //getting here means crossing the middle barrier. where row 1 is next to row 77
                return 1 + countSeperatorsBetweenDesks(1) + (77 - otherRow) / 2;
            }

            //getting here means that both Row and otherRow are between 60 and 77
            first = Math.Max(Row, otherRow);
            second = Math.Min(Row, otherRow);
            if (first % 2 == 1)
                return (first - second - 2) / 2;
            return (first - second - 1) / 2;
        }
    }
}
