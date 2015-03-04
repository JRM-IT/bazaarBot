using System.Collections.Generic;
namespace BazaarBot.Engine
{
    public class Agent
    {
        public int Id { get; private set; }			//unique integer identifier
        public string ClassId { get; private set; }	//string identifier, "famer", "woodcutter", etc.
        public Inventory Inventory { get; private set; }
        public float Money { get; set; }
        public bool Destroyed;

        public static float SIGNIFICANT = 0.25f;	//25% more or less is "significant"
        public static float SIG_IMBALANCE = 0.33f;
        public static float LOW_INVENTORY = 0.1f;		//10% of ideal inventory = "LOW"
        public static float HIGH_INVENTORY = 2.0f;	//200% of ideal inventory = "HIGH"

        public static float MIN_PRICE = 2f;		//lowest possible price
        public static float MAX_PRICE = 40f;

        private float _moneyLastRound;
        private Dictionary<string, Point> _priceBeliefs;
        private Dictionary<string, List<float>> _observedTradingRange;
        private int _lookback = 15;

        public Agent(int id, string classId, Inventory inventory, float money)
        {
            Id = id;
            ClassId = classId;
            Inventory = inventory ?? new Inventory();
            Money = money;
            _priceBeliefs = new Dictionary<string, Point>();
            _observedTradingRange = new Dictionary<string, List<float>>();
        }

        public void init(BazaarBot bazaar)
        {
            var list_commodities = bazaar.get_commodities_unsafe();
            foreach (var str in list_commodities)
            {
                var trades = new List<float>();

                var price = bazaar.GetPriceAverage(str, _lookback);
                trades.Add(price * 0.5f);
                trades.Add(price * 1.5f);	//push two fake trades to generate a range

                //set initial price belief & observed trading range
                _observedTradingRange.Add(str, trades);
                _priceBeliefs.Add(str, new Point(price * 0.5f, price * 1.5f));
            }
        }

        public void generate_offers(BazaarBot bazaar, string commodity)
        {
            Offer offer;
            float surplus = Inventory.Surplus(commodity);
            if (surplus >= 1)
            {
                offer = create_ask(bazaar, commodity, 1);
                if (offer != null)
                    bazaar.ask(offer);
            }
            else
            {
                float shortage = Inventory.Shortage(commodity);
                float space = Inventory.SpaceEmpty;
                float unit_size = Inventory.Size(commodity);

                if (shortage > 0 && space >= unit_size)
                {
                    float limit;
                    if ((shortage * unit_size) <= space)
                    { //enough space for ideal order
                        limit = shortage;
                    }
                    else
                    {								   //not enough space for ideal order
                        limit = (float)System.Math.Floor(space / shortage);
                    }

                    if (limit > 0)
                    {
                        offer = create_bid(bazaar, commodity, limit);
                        if (offer != null)
                        {
                            bazaar.bid(offer);
                        }
                    }
                }
            }
        }

        public void update_price_model(BazaarBot bazaar, string act, string commodity, bool success, float unit_price_ = 0)
        {

            if (success)
            {
                //Add this to my list of observed trades		
                var observed_trades = _observedTradingRange[commodity];
                observed_trades.Add(unit_price_);
            }

            var public_mean_price = bazaar.GetPriceAverage(commodity, 1);

            var belief = _priceBeliefs[commodity];
            var mean = (belief.X + belief.Y) / 2;
            var wobble = 0.05;

            var delta_to_mean = mean - public_mean_price;

            if (success)
            {
                if (act == "buy" && delta_to_mean > SIGNIFICANT)
                {			//overpaid
                    belief.X -= delta_to_mean / 2;							//SHIFT towards mean
                    belief.Y -= delta_to_mean / 2;
                }
                else if (act == "sell" && delta_to_mean < -SIGNIFICANT)
                {	//undersold
                    belief.X -= delta_to_mean / 2;							//SHIFT towards mean
                    belief.Y -= delta_to_mean / 2;
                }

                belief.X += (float)wobble * mean;	//increase the belief's certainty
                belief.Y -= (float)wobble * mean;
            }
            else
            {
                belief.X -= delta_to_mean / 2;	//SHIFT towards the mean
                belief.Y -= delta_to_mean / 2;

                var special_case = false;
                var stocks = QueryInventory(commodity);
                var ideal = Inventory.Ideal(commodity);

                if (act == "buy" && stocks < LOW_INVENTORY * ideal)
                {
                    //very low on inventory AND can't buy
                    wobble *= 2;			//bid more liberally
                    special_case = true;
                }
                else if (act == "sell" && stocks > HIGH_INVENTORY * ideal)
                {
                    //very high on inventory AND can't sell
                    wobble *= 2;			//ask more liberally
                    special_case = true;
                }

                if (!special_case)
                {
                    //Don't know what else to do? Check supply vs. demand
                    var asks = bazaar.GetAskAverage(commodity, 1);
                    var bids = bazaar.GetBidAverage(commodity, 1);

                    //supply_vs_demand: 0=balance, 1=all supply, -1=all demand
                    var supply_vs_demand = (asks - bids) / (asks + bids);

                    //too much supply, or too much demand
                    if (supply_vs_demand > SIG_IMBALANCE || supply_vs_demand < -SIG_IMBALANCE)
                    {
                        //too much supply: lower price
                        //too much demand: raise price

                        var new_mean = public_mean_price * (1 - supply_vs_demand);
                        delta_to_mean = mean - new_mean;

                        belief.X -= delta_to_mean / 2;	//SHIFT towards anticipated new mean
                        belief.Y -= delta_to_mean / 2;
                    }
                }

                belief.X -= (float)wobble * mean;	//decrease the belief's certainty
                belief.Y += (float)wobble * mean;
            }

            if (belief.X < MIN_PRICE)
                belief.X = MIN_PRICE;
            if (belief.Y < MIN_PRICE)
                belief.Y = MIN_PRICE;
            if (belief.Y > MAX_PRICE)
                belief.Y = MAX_PRICE;
            if (belief.X > MAX_PRICE)
                belief.X = MAX_PRICE;
        }

        public Offer create_bid(BazaarBot bazaar, string commodity, float limit)
        {
            var bid_price = determine_price_of(commodity);
            var ideal = determine_purchase_quantity(bazaar, commodity);

            //can't buy more than limit
            var quantity_to_buy = ideal > limit ? limit : ideal;
            if (quantity_to_buy > 0)
            {
                return new Offer(Id, commodity, quantity_to_buy, bid_price);
            }
            return null;
        }

        public Offer create_ask(BazaarBot bazaar, string commodity_, float limit_)
        {
            var ask_price = determine_price_of(commodity_);
            var ideal = determine_sale_quantity(bazaar, commodity_);

            //can't sell less than limit
            var quantity_to_sell = ideal < limit_ ? limit_ : ideal;
            if (quantity_to_sell > 0)
            {
                return new Offer(Id, commodity_, quantity_to_sell, ask_price);
            }
            return null;
        }

        public float QueryInventory(string commodity_)
        {
            return Inventory.Query(commodity_);
        }

        public void ChangeInventory(string commodity, float delta)
        {
            if (commodity == "money")
                Money += delta;
            else
                Inventory.Change(commodity, delta);
        }

        public float get_money_last()
        {
            return _moneyLastRound;
        }

        public float set_money_last(float f)
        {
            _moneyLastRound = f;
            return _moneyLastRound;
        }

        public float get_profit()
        {
            return Money - _moneyLastRound;
        }

        private float determine_price_of(string commodity_)
        {
            var belief = _priceBeliefs[commodity_];
            return rand_range(belief.X, belief.Y);
        }

        private float determine_sale_quantity(BazaarBot bazaar, string commodity_)
        {
            var mean = bazaar.GetPriceAverage(commodity_, _lookback);
            var trading_range = observe_trading_range(commodity_);
            if (trading_range != null)
            {
                var favorability = position_in_range(mean, trading_range.X, trading_range.Y);
                //position_in_range: high means price is at a high point

                var amount_to_sell = System.Math.Round(favorability * Inventory.Surplus(commodity_));
                if (amount_to_sell < 1)
                {
                    amount_to_sell = 1;
                }
                return (float)amount_to_sell;
            } return 0;
        }

        private float determine_purchase_quantity(BazaarBot bazaar, string commodity)
        {
            var mean = bazaar.GetPriceAverage(commodity, _lookback);
            var trading_range = observe_trading_range(commodity);
            if (trading_range != null)
            {
                var favorability = position_in_range(mean, trading_range.X, trading_range.Y);
                favorability = 1 - favorability;
                //do 1 - favorability to see how close we are to the low end

                var amount_to_buy = System.Math.Round(favorability * Inventory.Shortage(commodity));
                if (amount_to_buy < 1)
                {
                    amount_to_buy = 1;
                }
                return (float)amount_to_buy;
            } return 0;
        }

        private Point observe_trading_range(string commodity_)
        {
            var a = _observedTradingRange[commodity_];
            var pt = new Point(min_arr(a), max_arr(a));
            return pt;
        }

        private static float rand_range(float a, float b)
        {
            var min = a < b ? a : b;
            var max = a < b ? b : a;
            var range = max - min;
            return (float)BazaarBot.RNG.Next() * range + min;
        }

        private static float min_arr(List<float> a)
        {
            var min = 999999999f;
            foreach (var f in a)
            {
                if (f < min) { min = f; }
            }
            return min;
        }

        private static float max_arr(List<float> a)
        {
            var max = -999999999f;
            foreach (var f in a)
            {
                if (f > max) { max = f; }
            }
            return max;
        }

        private static float position_in_range(float value, float min, float max, bool clamp = true)
        {
            value -= min;
            max -= min;
            min = 0;
            value = (value / (max - min));
            if (clamp)
            {
                if (value < 0) { value = 0; }
                if (value > 1) { value = 1; }
            }
            return value;
        }
    }
}