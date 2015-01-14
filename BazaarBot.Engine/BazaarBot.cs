using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BazaarBot.Engine
{
    public class BazaarBot
    {
        public int num_commodities { get { return Commodities.Count; } }
        public int num_agents { get { return Agents.Count; } }
        public List<string> Commodities { get; private set; }
        Dictionary<string, Commodity> _commodityMap = new Dictionary<string, Commodity>();
        public Dictionary<string, AgentClass> AgentClasses { get; private set; }
        public List<Agent> Agents { get; private set; }
        Dictionary<string, List<Offer>> _bookBids = new Dictionary<string, List<Offer>>();
        Dictionary<string, List<Offer>> _bookAsks = new Dictionary<string, List<Offer>>();
        Dictionary<string, List<float>> _historyProfit = new Dictionary<string, List<float>>();

        private Dictionary<string, List<float>> history_price = new Dictionary<string, List<float>>();	//avg clearing price per good over time
        private Dictionary<string, List<float>> history_asks = new Dictionary<string,List<float>>();		//# ask (sell) offers per good over time
        private Dictionary<string, List<float>> history_bids = new Dictionary<string,List<float>>();		//# bid (buy) offers per good over time
        private Dictionary<string, List<float>> history_trades = new Dictionary<string,List<float>>();		//# units traded per good over time

        public void LoadJsonSettings(string fileName)
        {
            var data = JObject.Parse(File.ReadAllText(fileName));
            JToken dataCommodoties = null;
            JToken dataStartConditions = null;
            JToken dataAgents = null;

            Agents = new List<Agent>();
            Commodities = new List<string>();
            AgentClasses = new Dictionary<string, AgentClass>();

            foreach (var property in data.OfType<JProperty>())
            {
                switch (property.Name)
                {
                    case ("commodities"):
                        dataCommodoties = property.DeepClone();
                        
                        break;
                    case ("agents"):
                        dataAgents = property.DeepClone();
                        
                        break;
                    case ("start_conditions"):
                        dataStartConditions = property.DeepClone();
                        break;
                }
            }
            ParseCommodities(dataCommodoties as JProperty);
            ParseAgents(dataAgents as JProperty);
            ParseStartConditions(dataStartConditions as JProperty);
        }

        private void ParseStartConditions(JProperty property)
        {
            var agents = property.Children().SelectMany(p => p.Children<JProperty>()).First().First;
            var starts = agents.Children<JProperty>();
            var agent_index = 0;
            //Make given number of each agent type
            foreach (var item in starts)
            {
                var val = (int)item.Value;
                var agent_class = AgentClasses[item.Name];
                var inv = agent_class.GetStartInventory();
                var money = agent_class.money;

                for (int i = 0; i < val; i++)
                {
                    var a = new Agent(agent_index, item.Name, inv.Copy(), money);
                    a.init(this);
                    Agents.Add(a);
                    agent_index++;
                }
            }
        }

        private void ParseAgents(JProperty property)
        {
            AgentClasses = new Dictionary<string, AgentClass>();
            foreach (var a in property.First.Children())
            {
                //a.inventory.size = { };
                //foreach (var key in _map_commodities.Keys) {
                //    var c = _map_commodities[key];
                //    Reflect.setField(a.inventory.size, c.id, c.size);
                //}
                var ac = new AgentClass(a);
                AgentClasses[ac.id] = ac;
                _historyProfit[ac.id] = new List<float>();
            }

            //Make the agent list
            Agents = new List<Agent>();
        }

        private void ParseCommodities(JProperty property)
        {
            var children = property.Value.Children();
            foreach (var item in children)
            {
                var c = new Commodity();
                foreach (var j in item.Children<JProperty>())
                {
                    switch (j.Name)
                    {
                        case ("size"):
                            c.size = (float)j.Value;
                            break;
                        case ("id"):
                            c.id = (string)j.Value;
                            break;
                    }
                }

                Commodities.Add(c.id);
                _commodityMap[c.id] = new Commodity(c.id, c.size);
                history_price[c.id] = new List<float>();
                history_asks[c.id] = new List<float>();
                history_bids[c.id] = new List<float>();
                history_trades[c.id] = new List<float>();
                add_history_price(c.id, 1);		//start the bidding at $1!
                add_history_asks(c.id, 1);		//start history charts with 1 fake buy/sell bid
                add_history_bids(c.id, 1);
                add_history_trades(c.id, 1);
                _bookAsks[c.id] = new List<Offer>();
                _bookBids[c.id] = new List<Offer>();
            }
        }

    public void simulate(int rounds)
    {
		for (int round = 0 ; round < rounds; round++) {
			foreach (var agent in Agents) {
				agent.set_money_last(agent.Money);
				
				var ac = AgentClasses[agent.ClassId];				
				ac.logic.Perform(agent);
								
				foreach (var commodity in Commodities) {
					agent.generate_offers(this, commodity);
				}
			}
			foreach (var commodity in Commodities){
				resolve_offers(commodity);
			}
			foreach (var agent in Agents.ToList()) {
				if (agent.Money <= 0) {
					replaceAgent(agent);
				}
			}
		}
	}

    public void ask(Offer offer)
    {
        _bookAsks[offer.Commodity].Add(offer);
    }

    public void bid(Offer offer)
    {
        _bookBids[offer.Commodity].Add(offer);
    }

    /**
        * Returns the historical mean price of the given commodity over the last X rounds
        * @param	commodity_ string id of commodity
        * @param	range number of rounds to look back
        * @return
        */

    public float get_history_price_avg(string commodity, int range)
    {
        var list = history_price[commodity];
        return avg_list_f(list, range);
    }

    /**
        * Returns the historical profitability for the given agent class over the last X rounds
        * @param	class_ string id of agent class
        * @param	range number of rounds to look back
        * @return
        */

    public float get_history_profit_avg(string class_, int range)
    {
        var list = _historyProfit[class_];
        return avg_list_f(list, range);
    }

    /**
        * Returns the historical mean # of asks (sell offers) per round of the given commodity over the last X rounds
        * @param	commodity_ string id of commodity
        * @param	range number of rounds to look back
        * @return
        */

    public float get_history_asks_avg(string commodity_, int range)
    {
        var list = history_asks[commodity_];
        return avg_list_f(list, range);
    }

    /**
        * Returns the historical mean # of bids (buy offers) per round of the given commodity over the last X rounds
        * @param	commodity_ string id of commodity
        * @param	range number of rounds to look back
        * @return
        */

    public float get_history_bids_avg(string commodity_, int range)
    {
        var list = history_bids[commodity_];
        return avg_list_f(list, range);
    }

    public float get_history_trades_avg(string commodity_, int range)
    {
        var list = history_trades[commodity_];
        return avg_list_f(list, range);
    }

    public List<string> get_commodities_unsafe()
    {
        return Commodities;
    }

    public Commodity get_commodity_entry(string str)
    {
        if (_commodityMap.ContainsKey(str))
        {
            return _commodityMap[str].Copy();
        }
        return null;
    }

    private void resolve_offers(string commodity_ = "")
    {
		var bids = _bookBids[commodity_];
		var asks = _bookAsks[commodity_];		
		
		//shuffle the books
		shuffle(bids);
		shuffle(asks);
		
		bids.Sort(sort_decreasing_price);	//highest buying price first
		asks.Sort(sort_increasing_price);	//lowest selling price first
		
		int successful_trades = 0;		//# of successful trades this round
		float money_traded = 0;			//amount of money traded this round
		float units_traded = 0;			//amount of goods traded this round
		float avg_price;				//avg clearing price this round
		float num_asks= 0;
		float num_bids= 0;
		
		int failsafe = 0;
		
		for (int i = 0; i< bids.Count;i++)
        {
			num_bids += bids[i].Units;
		}
		
		for (int i = 0; i<asks.Count;i++)
        {
			num_asks += asks[i].Units;
		}
				
		//march through and try to clear orders
		while (bids.Count > 0 && asks.Count > 0) {	//while both books are non-empty
			var buyer = bids[0];
			var seller = asks[0];
			
			var quantity_traded = minf(seller.Units, buyer.Units);
			var clearing_price = avgf(seller.UnitPrice, buyer.UnitPrice);
						
			if (quantity_traded > 0) {
				//transfer the goods for the agreed price
				seller.Units -= quantity_traded;
				buyer.Units -= quantity_traded;
							
				transfer_commodity(commodity_, quantity_traded, seller.AgentId, buyer.AgentId);
				transfer_money(quantity_traded * clearing_price, seller.AgentId, buyer.AgentId);
									
				//update agent price beliefs based on successful transaction
				var buyer_a  = Agents[buyer.AgentId];
				var seller_a = Agents[seller.AgentId];
				buyer_a.update_price_model(this, "buy", commodity_, true, clearing_price);
				seller_a.update_price_model(this, "sell", commodity_, true, clearing_price);
				
				//log the stats
				money_traded += (quantity_traded * clearing_price);
				units_traded += quantity_traded;
				successful_trades++;							
			}
						
			if (seller.Units == 0) {	//seller is out of offered good
				asks.Remove(asks[0]);		//remove ask
				failsafe = 0;
			}
			if (buyer.Units == 0) {		//buyer is out of offered good
				bids.Remove(bids[0]);		//remove bid
				failsafe = 0;
			}
			
			failsafe++;
			
			if (failsafe > 1000) {
				Console.WriteLine("BOINK!");		
			}
		}
		
		//reject all remaining offers, 
		//update price belief models based on unsuccessful transaction
		while(bids.Count > 0){
			var buyer = bids[0];
			var buyer_a = Agents[buyer.AgentId];
			buyer_a.update_price_model(this,"buy",commodity_, false);
            bids.Remove(bids[0]);
		}
		while(asks.Count > 0){
			var seller = asks[0];
			var seller_a = Agents[seller.AgentId];
			seller_a.update_price_model(this,"sell",commodity_, false);
            asks.Remove(asks[0]);
		}
		
		//update history		
		
		add_history_asks(commodity_, num_asks);
		add_history_bids(commodity_, num_bids);
		add_history_trades(commodity_, units_traded);		
		
		if(units_traded > 0){
			avg_price = money_traded / (float)units_traded;
			add_history_price(commodity_, avg_price);		
		}else {
			//special case: none were traded this round, use last round's average price
			add_history_price(commodity_, get_history_price_avg(commodity_, 1));
			avg_price = get_history_price_avg(commodity_,1);
		}		
		
		Agents.Sort(sort_agent_alpha);
		var curr_class = "";
		var last_class = "";
        List<float> list = null;                                                                                    
		
        for (int i=0;i < Agents.Count;i++) {
			var a = Agents[i];		//get current agent
			curr_class = a.ClassId;			//check its class
			if (curr_class != last_class) {		//new class?
				if (list != null) {				//do we have a list built up?
					//log last class' profit
					add_history_profit(last_class, list_avg_f(list));	
				}
				list = new List<float>();		//make a new list
				last_class = curr_class;		
			}
			list.Add(a.get_profit());			//push profit onto list
		}	
		//add the last class too
		add_history_profit(last_class, list_avg_f(list));
		
		//sort by id so everything works again
		Agents.Sort(sort_agent_id);
		
	}

        private float list_avg_f(List<float> list)
        {
            float avg = 0;
            for (int j = 0; j < list.Count; j++)
            {
                avg += list[j];
            }
            avg /= list.Count;
            return avg;
        }

        private void replaceAgent(Agent agent)
        {
            var best_id = most_profitable_agent_class();

            //Special case to deal with very high demand-to-supply ratios
            //This will make them favor entering an underserved market over
            //Just picking the most profitable class
            var best_opportunity = get_best_market_opportunity();
            if (best_opportunity != "")
            {
                var best_opportunity_class = get_agent_class_that_makes_most(best_opportunity);
                if (best_opportunity_class != "")
                {
                    best_id = best_opportunity_class;
                }
            }

            var agent_class = AgentClasses[best_id];
            var new_agent = new Agent(agent.Id, best_id, agent_class.GetStartInventory(), agent_class.money);
            new_agent.init(this);
            Agents[agent.Id] = new_agent;
            agent.Destroyed = true;
        }

        private string get_agent_class_that_makes_most(string commodity_)
        {
            float best_amount = 0;
            var best_class = "";
            foreach (var key in AgentClasses.Keys)
            {
                var ac = AgentClasses[key];
                var amount = ac.logic.GetProduction(commodity_);
                if (amount > best_amount)
                {
                    best_amount = amount;
                    best_class = ac.id;
                }
            }
            return best_class;
        }

        private string get_agent_class_with_most(string commodity_)
        {
            var amount = 0f;
            var best_amount = 0f;
            var best_class = "";
            foreach (var key in AgentClasses.Keys)
            {
                amount = get_avg_inventory(key, commodity_);
                if (amount > best_amount)
                {
                    best_amount = amount;
                    best_class = key;
                }
            }
            return best_class;
        }

        private float get_avg_inventory(string agent_id, string commodity_)
        {
            var list = Agents.Where(p => p.ClassId == agent_id);
            var amount = 0f;
            foreach (var agent in list)
            {
                amount += agent.QueryInventory(commodity_);
            }
            amount /= list.Count();
            return amount;
        }

        /**
         * Get the market with the highest demand/supply ratio over time
         * @param   minimum the minimum demand/supply ratio to consider an opportunity
         * @param	range number of rounds to look back
         * @return
         */

        private string get_best_market_opportunity(float minimum = 1.5f, int range = 10)
        {
            var best_market = "";
            var best_ratio = -999999f;
            foreach (var commodity in Commodities)
            {
                var asks = get_history_asks_avg(commodity, range);
                var bids = get_history_bids_avg(commodity, range);
                var ratio = 0f;
                if (asks == 0 && bids > 0)
                {
                    ratio = 9999999999999999;
                }
                else
                {
                    ratio = bids / asks;
                }
                if (ratio > minimum && ratio > best_ratio)
                {
                    best_ratio = ratio;
                    best_market = commodity;
                }
            }
            return best_market;
        }

        private string most_profitable_agent_class(int range = 10)
        {
            var best = -99999f;
            var best_id = "";
            foreach (var ac_id in AgentClasses.Keys)
            {
                var val = get_history_profit_avg(ac_id, range);
                if (val > best)
                {
                    best_id = ac_id;
                    best = val;
                }
            }
            return best_id;
        }

        private AgentClass get_agent_class(string str)
        {
            foreach (var ac in AgentClasses.Values)
            {
                if (ac.id== str)
                    return ac;
            }
            return null;
        }

        private void add_history_profit(string agent_class_, float f)
        {
            var list = _historyProfit[agent_class_];
            list.Add(f);
        }

        private void add_history_asks(string commodity_, float f)
        {
            var list = history_asks[commodity_];
            list.Add(f);
        }

        private void add_history_bids(string commodity_, float f)
        {
            var list = history_bids[commodity_];
            list.Add(f);
        }

        private void add_history_trades(string commodity_, float f)
        {
            var list = history_trades[commodity_];
            list.Add(f);
        }

        private void add_history_price(string commodity_, float p)
        {
            var list = history_price[commodity_];
            list.Add(p);
        }

        private void transfer_commodity(string commodity_, float units_, int seller_id, int buyer_id)
        {
            var seller = Agents[seller_id];
            var buyer = Agents[buyer_id];
            seller.ChangeInventory(commodity_, -units_);
            buyer.ChangeInventory(commodity_, units_);
        }

        private void transfer_money(float amount_, int seller_id, int buyer_id)
        {
            var seller = Agents[seller_id];
            var buyer = Agents[buyer_id];
            seller.Money += amount_;
            buyer.Money -= amount_;
        }

        private static int sort_agent_id(Agent a, Agent b)
        {
            if (a.Id < b.Id) return -1;
            if (a.Id > b.Id) return 1;
            return 0;
        }

        private static int sort_agent_alpha(Agent a, Agent b)
        {
            return string.Compare(a.ClassId, b.ClassId);
        }

        private static int sort_decreasing_price(Offer a, Offer b)
        {
            //Decreasing means: highest first
            if (a.UnitPrice < b.UnitPrice) return 1;
            if (a.UnitPrice > b.UnitPrice) return -1;
            return 0;
        }

        private static int sort_increasing_price(Offer a, Offer b)
        {
            //Increasing means: lowest first
            if (a.UnitPrice > b.UnitPrice) return 1;
            if (a.UnitPrice < b.UnitPrice) return -1;
            return 0;
        }

        private static float avgf(float a, float b)
        {
            return (a + b) / 2;
        }

        private static float minf(float a, float b)
        {
            return a < b ? a : b;
        }

        private static float maxf(float a, float b)
        {
            return a > b ? a : b;
        }

        private static int mini(int a, int b)
        {
            return a < b ? a : b;
        }

        private static int maxi(int a, int b)
        {
            return a > b ? a : b;
        }

        private static List<T> shuffle<T>(List<T> list)
        {
            for (int i = 0; i < list.Count - 1; i++)
            {
                var ii = (list.Count - 1) - i;
                if (ii > 1)
                {
                    var j = RNG.Int(0, ii);
                    var temp = list[j];
                    list[j] = list[ii];
                    list[ii] = temp;
                }
            }
            return list;
        }

        private static float avg_list_f(List<float> list, int range)
        {
            float avg = 0;
            var length = list.Count;
            if (length < range)
            {
                range = length;
            }
            for (int i = 0; i < range; i++)
            {
                avg += list[length - 1 - i];
            }
            avg /= (float)range;
            return avg;
        }

        private static float avg_list_i(List<int> list, int range)
        {
            float avg = 0;
            int length = list.Count;
            if (length < range)
            {
                range = length;
            }
            for (int i = 0; i < range; i++)
            {
                avg += list[length - 1 - i];
            }
            avg /= (float)range;
            return avg;
        }
    }
}