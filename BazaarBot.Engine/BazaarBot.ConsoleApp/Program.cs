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
                key = Console.ReadKey().Key;
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
            var report = bazaar.get_marketReport(1);
            Console.WriteLine();
            Console.WriteLine(report.str_list_commodity);
            Console.WriteLine(report.str_list_commodity_asks);
            Console.WriteLine(report.str_list_commodity_bids);
            Console.WriteLine(report.str_list_commodity_trades);
            Console.WriteLine(report.str_list_commodity_prices);
            sw.Stop();
            var time = sw.ElapsedMilliseconds;
            var avg = (float)time / (float)benchmark / 1000f;
            Console.WriteLine("Rounds: {0}, Commodities: {1}, Agents: {2}, Time: {3}, Avg Time: {4}", benchmark, bazaar.num_commodities, bazaar.num_agents, time, avg);
        }

        private static void Advance()
        {
            bazaar.simulate(1);
            var report = bazaar.get_marketReport(1);
            Console.WriteLine(report);
        }
    }
}
