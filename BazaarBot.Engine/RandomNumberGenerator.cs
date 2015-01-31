using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BazaarBot.Engine
{
    public class StandardRandomNumberGenerator : IRandomNumberGenerator
    {
        static Random rng;

        public StandardRandomNumberGenerator(int seed)
        {
            rng = new Random(seed);
        }

        public double Next()
        {
            return rng.NextDouble();
        }

        public int Int(int minx, int max)
        {
            return rng.Next(minx, max);
        }
    }
}
