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
        const int BENCHMARK_ROUND_COUNT = 30;

        static Engine.BazaarBot bazaar = new Engine.BazaarBot(0);
        static void Main(string[] args)
        {
            bazaar.LoadJsonSettings("settings.json");
            Console.WriteLine(new MarketReport(bazaar));
            MenuLoop();
        }

        private static void MenuLoop()
        {
            var key = default(ConsoleKey);
            while (key != ConsoleKey.Q)
            {
                Console.WriteLine("Round {0:N0}, (B)enchmark, (A)dvance or (Q)uit", bazaar.TotalRounds);
                key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.B)
                    Benchmark(BENCHMARK_ROUND_COUNT);
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
            Console.WriteLine("Rounds: {0}, Commodities: {1}, Agents: {2}, Time: {3}, Avg Time: {4}\n", benchmark, bazaar.CommodityClasses.Count, bazaar.Agents.Count, time, avg);
        }

        private static void Advance()
        {
            Console.Clear();
            bazaar.simulate(1);
            Console.WriteLine(new MarketReport(bazaar));
        }
    }
}
