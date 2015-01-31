using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BazaarBot.Engine
{
    public interface IRandomNumberGenerator
    {
        double Next();
        
        int Int(int min, int max);
    }
}
