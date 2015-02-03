using System;
using System.Collections.Generic;
using System.Linq;

namespace BazaarBot.Engine
{
    public class MarketReport
    {
        List<string> _commodities;
        List<string> _commodityPrices = new List<string> { "Price" };
        List<string> _commodityTrades = new List<string> { "Trades" };
        List<string> _commodityAsks = new List<string> { "Supply" };
        List<string> _commodityBids = new List<string> { "Demand" };
        List<string> _agents = new List<string> { "" };
        List<string> _agentCount = new List<string> { "Count" };
        List<string> _agentMoney = new List<string> { "Money" };
        List<string> _agentProfit = new List<string> { "Profit" };

        public List<float> _inventory = new List<float>();

        public MarketReport(BazaarBot bazaar)
        {
            _commodities = new List<string> {"R: " + bazaar.TotalRounds.ToString()};
            var rounds = 1;
            foreach (var commodity in bazaar.CommodityClasses)
            {
                _commodities.Add(commodity);
                _commodityPrices.Add(bazaar.GetPriceAverage(commodity, rounds).ToString("N2"));
                _commodityAsks.Add(bazaar.GetAskAverage(commodity, rounds).ToString());
                _commodityBids.Add(bazaar.GetBidAverage(commodity, rounds).ToString());
                _commodityTrades.Add(bazaar.GetTradeAverage(commodity, rounds).ToString());
            }

            foreach (var agentKey in bazaar.AgentClasses.Keys)
            {
                _agents.Add(agentKey);
                _agentProfit.Add(bazaar.GetProfitAverage(agentKey, rounds).ToString("N2"));

                var agents = bazaar.Agents.Where(p => p.ClassId == agentKey);
                
                _agentCount.Add(agents.Count().ToString("N0"));
                _agentMoney.Add(BazaarBot.Average(agents.Select(p => p.Money)).ToString("N0"));


                var inventory = Enumerable.Repeat(0f, bazaar.CommodityClasses.Count).ToArray();
                foreach (var a in agents)
                {
                    for (int i = 0; i < bazaar.CommodityClasses.Count; i++)
                    {
                        inventory[i] += a.QueryInventory(bazaar.CommodityClasses[i]);
                    }
                }
                _inventory.AddRange(inventory.Select(p => (p > 0 ? p / agents.Count() : 0)));
                
            }

        }

        int _commodityPadding;
        int _agentPadding;

        public string[][] GetData()
        {
            var result = new List<string[]>();
            result.Add(_commodities.ToArray());
            result.Add(_commodityPrices.ToArray());
            result.Add(_commodityTrades.ToArray());
            result.Add(_commodityAsks.ToArray());
            result.Add(_commodityBids.ToArray());
            result.Add(_agents.ToArray());
            result.Add(_agentCount.ToArray());
            result.Add(_agentMoney.ToArray());
            result.Add(_agentProfit.ToArray());
            result.AddRange(GetInventory());
            return result.ToArray();
        }

        public override string ToString()
        {
            _commodityPadding = _commodities.Union(_commodityPrices).Union(_commodityTrades).Union(_commodityAsks).Union(_commodityBids).Max(p => p.Length) + 1;
            
            var result = string.Join("\n", 
                string.Join("",_commodities.Select(p => p.PadRight(_commodityPadding))),
                string.Join("",_commodityPrices.Select(p => p.PadRight(_commodityPadding))),
                string.Join("",_commodityTrades.Select(p => p.PadRight(_commodityPadding))),
                string.Join("",_commodityAsks.Select(p => p.PadRight(_commodityPadding))),
                string.Join("",_commodityBids.Select(p => p.PadRight(_commodityPadding))));

            result += "\n\n";

            _agentPadding = _agents.Union(_agentCount).Union(_agentMoney).Union(_agentProfit).Max(p => p.Length) + 1;

            result += string.Join("\n",
                string.Join("", _agents.Select(p => p.PadRight(_agentPadding))),
                string.Join("", _agentCount.Select(p => p.PadRight(_agentPadding))),
                string.Join("", _agentMoney.Select(p => p.PadRight(_agentPadding))),
                string.Join("", _agentProfit.Select(p => p.PadRight(_agentPadding))));

            result += "\n\n";

            result += string.Join("\n", GetInventoryAsStrings());

            result += "\n";

            return result;
        }

        private IEnumerable<string[]> GetInventory()
        {
            yield return new string[] { "" }.Union(_commodities.Skip(1)).ToArray();
            for (int i = 0; i < _agents.Count - 1; i++)
            {
                var result = _inventory.Skip(i * (_commodities.Count - 1)).Take(_commodities.Count - 1).Select(p => p.ToString("N2")).ToList();
                result.Insert(0, _agents[i + 1]);
                //var resultStr = new string[] { }.UnionAll(result);
                var resultStr = result;
                yield return resultStr.ToArray();
            }
        }

        private IEnumerable<string> GetInventoryAsStrings()
        {
            var pad = Math.Max(_commodityPadding, _agentPadding );
            foreach (var line in GetInventory())
            {
                yield return string.Join("", line.Select(p => p.PadRight(pad)));
            }
        }
    }
}