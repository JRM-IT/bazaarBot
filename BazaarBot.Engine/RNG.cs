using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BazaarBot.Engine
{
    class RNG
    {
        public static Random rng = new Random(DateTime.Now.Second);

        public static double Next()
        {
            return rng.NextDouble();
        }

        internal static int Int(int p, int ii)
        {
            return rng.Next(p, ii);
        }
    }
}
