using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BazaarBot.Engine
{
    public static class JSONParser
    {
        public static void LoadJsonSettings(BazaarBot bazaar, string fileName)
        {
            var node = JSON.Parse(File.ReadAllText(fileName));

            bazaar.Agents = new List<Agent>();
            bazaar.CommodityClasses = new List<string>();
            bazaar.AgentClasses = new Dictionary<string, AgentClass>();

            ParseCommodities(bazaar, node["commodities"]);
            ParseAgents(bazaar, node["agents"]);
            ParseStartConditions(bazaar, node["start_conditions"]);
        }

        private static void ParseStartConditions(BazaarBot bazaar, JSONNode node)
        {
            bazaar.Agents = new List<Agent>();
            var starts = node["agents"].ToString().RemoveAll("\"{}").Split(',');
            var agent_index = 0;
            //Make given number of each agent type
            foreach (var item in starts)
            {
                var split = item.Split(':');
                var name = split[0].Trim();
                var val = int.Parse(split[1]);
                var agent_class = bazaar.AgentClasses[name];
                var inv = agent_class.GetStartInventory();
                var money = agent_class.money;

                for (int i = 0; i < val; i++)
                {
                    var a = new Agent(agent_index, name, inv.Copy(), money);
                    a.init(bazaar);
                    bazaar.Agents.Add(a);
                    agent_index++;
                }
            }
        }

        private static void ParseAgents(BazaarBot bazaar, JSONNode node)
        {
            bazaar.AgentClasses = new Dictionary<string, AgentClass>();
            foreach (var agent in node.AsArray.Children)
            {
                //a.inventory.size = { };
                //foreach (var key in _map_commodities.Keys) {
                //    var c = _map_commodities[key];
                //    Reflect.setField(a.inventory.size, c.id, c.size);
                //}
                var agentClass = JSONParser.ParseAgentClass(agent);
                bazaar.AgentClasses[agentClass.id] = agentClass;
                bazaar.ProfitHistory[agentClass.id] = new List<float>();
            }
        }

        private static void ParseCommodities(BazaarBot bazaar, JSONNode node)
        {
            foreach (var commodity in node.AsArray.Children)
            {
                var id = commodity["id"].Value;
                bazaar.CommodityClasses.Add(id);
                bazaar.PriceHistory[id] = new List<float>();
                bazaar.AskHistory[id] = new List<float>();
                bazaar.BidHistory[id] = new List<float>();
                bazaar.VarHistory[id] = new List<float>();
                bazaar.TradeHistory[id] = new List<float>();
                bazaar.PriceHistory[id].Add(1);    //start the bidding at $1!
                bazaar.AskHistory[id].Add(1);      //start history charts with 1 fake buy/sell bid
                bazaar.BidHistory[id].Add(1);
                bazaar.TradeHistory[id].Add(1);
                bazaar.Asks[id] = new List<Offer>();
                bazaar.Bids[id] = new List<Offer>();
            }
        }

        public static AgentLogicNode ParseAgentLogicNode(JSONNode data)
        {
            if (data["condition"] != null)
            {
                var result = new AgentLogicNode
                {
                    IsLeaf = false,
                    Conditions = data["condition"].AsArray.Children.Select(p => new AgentCondition(p)).ToArray(),
                    Parameters = data["param"].AsArray.Children.Select(p=> p.Value).ToArray(),
                    NodeTrue = GetLogicNode(data["if_true"]),
                    NodeFalse = GetLogicNode(data["if_false"])
                };

                if (result.Conditions.Length != result.Parameters.Length)
                    result.Conditions = Enumerable.Repeat(result.Conditions.First(), result.Parameters.Length).ToArray();
                
                return result;
            }
            else
            {
                return new AgentLogicNode
                {
                    IsLeaf = true,
                    Actions = data["action"].AsArray.Children.Select(p => ParseAgentLogicAction(p)).ToList()
                };
            }
        }
        

        private static AgentLogicNode GetLogicNode(JSONNode node)
        {
            if (node != null)
                return ParseAgentLogicNode(node);
            return null;
        }

        public static AgentClass ParseAgentClass(JSONNode node)
        {
            var ideal = new Dictionary<string, float>();
            var start = new Dictionary<string, float>();

            var id = node["id"].Value;
            var money = node["money"].AsInt;
            var inventory = node["inventory"].AsObject;
            Populate(inventory["start"], start);
            Populate(inventory["ideal"], ideal);
            var maxInventorySize = inventory["max_size"].AsInt;

            var logic = new AgentLogic(node["logic"]);
            return new AgentClass(ideal, start, maxInventorySize)
            {
                id = id,
                money = money,
                logic = logic
            }; 
        }

        private static void Populate(JSONNode node, Dictionary<string,float> dictionary)
        {
            var items = node.ToString().RemoveAll("\"{}").Split(',');
            foreach (var item in items)
            {
                var split = item.Split(':');
                dictionary[split[0].Trim()] = float.Parse(split[1]);
            }
        }

        public static AgentLogicAction ParseAgentLogicAction(JSONNode node)
        {
            var action = GetAction(node, "produce") ?? GetAction(node, "consume") ?? GetAction(node, "transform");
            if (node["amount"] != null)
                action.amounts = node["amount"].AsArray.Children.Select(p => p.Value == "all" ? -1f : p.AsFloat).ToList();
            else
                action.amounts = new List<float>();

            if (node["chance"] != null)
                action.Chances = node["chance"].AsArray.Children.Select(p => p.AsFloat).ToList();
            else
                action.Chances = new List<float>();

            if (action.action == "transform")
            {
                if (node["efficiency"] != null)
                    action.efficiency = node["efficiency"].AsArray.Children.Select(p => p.AsFloat).ToList();
                else
                    action.efficiency = new List<float>();
            }

            action.results = node["into"].AsArray.Children.Select(p => p.Value).ToList();

            for (int i = 0; i < action.Targets.Count(); i++)
            {
                if (i > action.amounts.Count - 1)
                {
                    action.amounts.Add(1);			//if item is specified but amount is not, amount is 1
                }
                if (i > action.Chances.Count - 1)
                {
                    action.Chances.Add(1);			//if item is specified but chance is not, chance is 1
                }
                if (action.action == "transform")
                {
                    if (i > action.efficiency.Count - 1)
                    {
                        action.efficiency.Add(1);			//if item is specified but efficiency is not, efficiency is 1
                    }
                }
            }
            return action;
        }

        private static AgentLogicAction GetAction(JSONNode node, string actionName)
        {
            if (node[actionName] != null)
                return new AgentLogicAction
                {
                    action = actionName,
                    Targets = node[actionName].AsArray.Children.Select(p => p.Value).ToArray()
                };
            return null;
        }
    }
}
