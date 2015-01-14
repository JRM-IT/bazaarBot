using BazaarBot.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BazaarBot.ConsoleApp
{
    class Program
    {
        static Engine.BazaarBot bazaar = new Engine.BazaarBot();
        static void Main(string[] args)
        {
            bazaar.LoadJsonSettings("settings.json");
            var key = ConsoleKey.A;
            while (key != ConsoleKey.Q)
            {
                Console.WriteLine("(B)enchmark or (A)dvance");
                key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.B)
                    Benchmark(30);
                else if (key == ConsoleKey.A)
                    Advance();
            }
        }

        private static void Benchmark(int benchmark)
        {
            Console.Clear();
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            bazaar.simulate(benchmark);
            Console.WriteLine(new MarketReport(bazaar));
            sw.Stop();
            var time = sw.ElapsedMilliseconds;
            var avg = (float)time / (float)benchmark / 1000f;
            Console.WriteLine("Rounds: {0}, Commodities: {1}, Agents: {2}, Time: {3}, Avg Time: {4}\n", benchmark, bazaar.num_commodities, bazaar.num_agents, time, avg);
        }

        private static void Advance()
        {
            Console.Clear();
            bazaar.simulate(1);
            Console.WriteLine(new MarketReport(bazaar));
        }
    }
}
