using System.Collections.Generic;
using System.Linq;

namespace BazaarBot.Engine
{
    public class MarketReport
    {
        List<string> _commodities = new List<string> { "Stuff" };
        List<string> _commodityPrices = new List<string> { "Price" };
        List<string> _commodityTrades = new List<string> { "Trades" };
        List<string> _commodityAsks = new List<string> { "Supply" };
        List<string> _commodityBids = new List<string> { "Demand" };
        List<string> _agents = new List<string> { "Classes" };
        List<string> _agentCount = new List<string> { "Count" };
        List<string> _agentMoney = new List<string> { "Profit" };
        List<string> _agentProfit = new List<string> { "Money" };

        public List<string> _inventory = new List<string>();

        public MarketReport(BazaarBot bazaar)
        {
            var rounds = 1;
            foreach (var commodity in bazaar.Commodities)
            {
                _commodities.Add(commodity);
                _commodityPrices.Add(bazaar.get_history_price_avg(commodity, rounds).ToString("N2"));
                _commodityAsks.Add(bazaar.get_history_asks_avg(commodity, rounds).ToString());
                _commodityBids.Add(bazaar.get_history_bids_avg(commodity, rounds).ToString());
                _commodityTrades.Add(bazaar.get_history_trades_avg(commodity, rounds).ToString());
            }

            foreach (var key in bazaar.AgentClasses.Keys)
            {
                var inventory = new List<float>();
                foreach (var str in bazaar.Commodities)
                {
                    inventory.Add(0);
                }
                _agents.Add(key);
                var profit = bazaar.get_history_profit_avg(key, rounds);
                _agentProfit.Add(profit.ToString("N2"));

                var agents = bazaar.Agents.Where(p => p.ClassId == key);
                float money = 0;

                foreach (var a in agents)
                {
                    money += a.Money;
                    for (int i = 0; i < bazaar.Commodities.Count; i++)
                    {
                        inventory[i] += a.QueryInventory(bazaar.Commodities[i]);
                    }
                }

                money /= agents.Count();
                for (int i = 0; i < bazaar.Commodities.Count; i++)
                {
                    inventory[i] /= agents.Count();
                    _inventory.Add(inventory[i].ToString("N1"));
                }

                _agentCount.Add(agents.Count().ToString("N0"));
                _agentMoney.Add(money.ToString("N0"));
            }

        }

        public override string ToString()
        {
            var pad = _commodities.Union(_commodityPrices).Union(_commodityTrades).Union(_commodityAsks).Union(_commodityBids).Max(p => p.Length) + 1;
            
            var result = string.Join("\n", 
                string.Join("",_commodities.Select(p => p.PadRight(pad))),
                string.Join("",_commodityPrices.Select(p => p.PadRight(pad))),
                string.Join("",_commodityTrades.Select(p => p.PadRight(pad))),
                string.Join("",_commodityAsks.Select(p => p.PadRight(pad))),
                string.Join("",_commodityBids.Select(p => p.PadRight(pad))));

            result += "\n";

            pad = _agents.Union(_agentCount).Union(_agentMoney).Union(_agentProfit).Max(p => p.Length) + 1;

            result += string.Join("\n",
                string.Join("", _agents.Select(p => p.PadRight(pad))),
                string.Join("", _agentCount.Select(p => p.PadRight(pad))),
                string.Join("", _agentMoney.Select(p => p.PadRight(pad))),
                string.Join("", _agentProfit.Select(p => p.PadRight(pad))));

            return result;
        }
    }
}