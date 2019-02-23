using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Collections.Concurrent;


namespace GASeatingProblem
{
    public static class GlobalRandom
    {
        //public static readonly Random RNG = new Random();
        public static Random RNG = new Random();

        private static int seedCounter = Int32.MinValue;
        public static Random GetLocalRNG()
        {
            return new Random(seedCounter++);
            //return new Random(GlobalRandom.RNG.Next());
        }

//        private static int next = 0;
//        private static Random[] randPool =
//        {
//            new Random(DateTime.Now.Millisecond * 17),
//            new Random(DateTime.Now.Millisecond * 17 + DateTime.Now.Second * 19),
//            new Random(DateTime.Now.Second * DateTime.Now.Millisecond),
//            new Random(DateTime.Now.Millisecond * 11),
//
//            new Random(),
//            new Random(DateTime.Now.Millisecond * DateTime.Now.Millisecond),
//            new Random(DateTime.Now.Millisecond + 600),
//            new Random((int) DateTime.Now.Ticks)
//        };
        
        public static void YatesShuffle<T>(List<T> list, Random RNG = null)
        {
            if (RNG == null) RNG = GlobalRandom.RNG;

            for (int i = 0; i < list.Count; i++)
            {
                int j = RNG.Next(i, list.Count); // Don't select from the entire list on subsequent loops
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
