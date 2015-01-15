using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BazaarBot.Engine
{
    class RNG
    {
        public static Random rng;

        public static double Next()
        {
            return rng.NextDouble();
        }

        public static int Int(int p, int ii)
        {
            return rng.Next(p, ii);
        }

        public static void Seed(int seed)
        {
            rng = new Random(seed);
        }
    }
}
