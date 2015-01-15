using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BazaarBot.Engine
{
    public class BazaarBot
    {
        public int TotalRounds { get; private set; }

        public List<string> CommodityClasses { get; private set; }
        
        public Dictionary<string, AgentClass> AgentClasses { get; private set; }
        public List<Agent> Agents { get; private set; }

        Dictionary<string, List<Offer>> _bids = new Dictionary<string, List<Offer>>();
        Dictionary<string, List<Offer>> _asks = new Dictionary<string, List<Offer>>();
        Dictionary<string, List<float>> _profitHistory = new Dictionary<string, List<float>>();
        Dictionary<string, List<float>> _priceHistory = new Dictionary<string, List<float>>();	//avg clearing price per good over time
        Dictionary<string, List<float>> _askHistory = new Dictionary<string,List<float>>();		//# ask (sell) offers per good over time
        Dictionary<string, List<float>> _bidHistory = new Dictionary<string,List<float>>();		//# bid (buy) offers per good over time
        Dictionary<string, List<float>> _tradeHistory = new Dictionary<string,List<float>>();   //# units traded per good over time

        public BazaarBot(int seed)
        {
            RNG.Seed(seed);
        }

        public void LoadJsonSettings(string fileName)
        {
            var data = JObject.Parse(File.ReadAllText(fileName));
            JToken dataCommodoties = null;
            JToken dataStartConditions = null;
            JToken dataAgents = null;

            Agents = new List<Agent>();
            CommodityClasses = new List<string>();
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
            Agents = new List<Agent>();
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
                var agentClass = new AgentClass(a);
                AgentClasses[agentClass.id] = agentClass;
                _profitHistory[agentClass.id] = new List<float>();
            }
        }

        private void ParseCommodities(JProperty property)
        {
            var commodities = property.Value.ToObject<Commodity[]>();
            
            foreach (var c in commodities)
            {
                CommodityClasses.Add(c.id);
                _priceHistory[c.id] = new List<float>();
                _askHistory[c.id] = new List<float>();
                _bidHistory[c.id] = new List<float>();
                _tradeHistory[c.id] = new List<float>();
                _priceHistory[c.id].Add(1);    //start the bidding at $1!
                _askHistory[c.id].Add(1);      //start history charts with 1 fake buy/sell bid
                _bidHistory[c.id].Add(1);
                _tradeHistory[c.id].Add(1);
                _asks[c.id] = new List<Offer>();
                _bids[c.id] = new List<Offer>();
            }
        }

        public void simulate(int rounds)
        {
		    for (int round = 0 ; round < rounds; round++) {
                TotalRounds++;
			    foreach (var agent in Agents) {
				    agent.set_money_last(agent.Money);
				
				    var ac = AgentClasses[agent.ClassId];				
				    ac.logic.Perform(agent);
								
				    foreach (var commodity in CommodityClasses) {
					    agent.generate_offers(this, commodity);
				    }
			    }
			    foreach (var commodity in CommodityClasses){
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
            _asks[offer.Commodity].Add(offer);
        }

        public void bid(Offer offer)
        {
            _bids[offer.Commodity].Add(offer);
        }

        public float GetPriceAverage(string commodity, int range)
        {
            return Average(_priceHistory[commodity], range);
        }

        public float GetProfitAverage(string commodity, int range)
        {
            var list = _profitHistory[commodity];
            return Average(list, range);
        }

        public float GetAskAverage(string commodity, int range)
        {
            return Average(_askHistory[commodity], range);
        }

        public float GetBidAverage(string commodity, int range)
        {
            return Average(_bidHistory[commodity], range);
        }

        public float GetTradeAverage(string commodity, int range)
        {
            var list = _tradeHistory[commodity];
            return Average(list, range);
        }

        public List<string> get_commodities_unsafe()
        {
            return CommodityClasses;
        }

        private void resolve_offers(string commodity = "")
        {
		    var bids = _bids[commodity];
		    var asks = _asks[commodity];		
		
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
			
			    var quantityTraded = Math.Min(seller.Units, buyer.Units);
			    var clearingPrice = avgf(seller.UnitPrice, buyer.UnitPrice);
						
			    if (quantityTraded > 0) {
				    //transfer the goods for the agreed price
                    seller.Trade(quantityTraded);
                    buyer.Trade(quantityTraded);
							
				    transfer_commodity(commodity, quantityTraded, seller.AgentId, buyer.AgentId);
				    transfer_money(quantityTraded * clearingPrice, seller.AgentId, buyer.AgentId);
									
				    //update agent price beliefs based on successful transaction
				    var buyer_a  = Agents[buyer.AgentId];
				    var seller_a = Agents[seller.AgentId];
				    buyer_a.update_price_model(this, "buy", commodity, true, clearingPrice);
				    seller_a.update_price_model(this, "sell", commodity, true, clearingPrice);
				
				    //log the stats
				    money_traded += (quantityTraded * clearingPrice);
				    units_traded += quantityTraded;
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
			    buyer_a.update_price_model(this,"buy",commodity, false);
                bids.Remove(bids[0]);
		    }
		    while(asks.Count > 0){
			    var seller = asks[0];
			    var seller_a = Agents[seller.AgentId];
			    seller_a.update_price_model(this,"sell",commodity, false);
                asks.Remove(asks[0]);
		    }
		
		    //update history		
            _askHistory[commodity].Add(num_asks);
		    _bidHistory[commodity].Add(num_bids);
            _tradeHistory[commodity].Add(units_traded);
		
		    if(units_traded > 0){
			    avg_price = money_traded / (float)units_traded;
                _priceHistory[commodity].Add(avg_price);
		    }else {
			    //special case: none were traded this round, use last round's average price
                _priceHistory[commodity].Add(GetPriceAverage(commodity, 1));
			    avg_price = GetPriceAverage(commodity,1);
		    }		
		
		    Agents.Sort(sort_agent_alpha);
		    var curr_class = "";
		    var last_class = "";
            List<float> list = null;                                                                                    
		
            for (int i=0;i < Agents.Count;i++) {
			    var a = Agents[i];		//get current agent
			    curr_class = a.ClassId;			//check its class
			    if (curr_class != last_class) {		//new class?
                    if (list != null) //do we have a list built up?
                    {				
					    //log last class' profit
                        _profitHistory[last_class].Add(Average(list));
				    }
				    list = new List<float>();		//make a new list
				    last_class = curr_class;		
			    }
			    list.Add(a.get_profit());			//push profit onto list
		    }	
		    //add the last class too
            _profitHistory[last_class].Add(Average(list));
		
		    //sort by id so everything works again
		    Agents.Sort(sort_agent_id);
		
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
            foreach (var commodity in CommodityClasses)
            {
                var asks = GetAskAverage(commodity, range);
                var bids = GetBidAverage(commodity, range);
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
                var val = GetProfitAverage(ac_id, range);
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

        public static float Average(IEnumerable<float> values)
        {
            return Average(values, values.Count());
        }

        private static float Average(IEnumerable<float> values, int range)
        {
            return values.Any() ? values.Reverse().Take(range).Average() : 0;
        }

        private static float Average(IEnumerable<int> values, int range)
        {
            return Average(values.Select(p => (float)p), range);
        }
    }
}