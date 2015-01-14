
using System.Collections.Generic;
namespace BazaarBot.Engine
{
    public class MarketReport
    {
        public string str_list_commodity = "";
        public string str_list_commodity_prices = "";
        public string str_list_commodity_trades = "";
        public string str_list_commodity_asks = "";
        public string str_list_commodity_bids = "";
        public string str_list_agent = "";
        public string str_list_agent_count = "";
        public string str_list_agent_money = "";
        public string str_list_agent_profit = "";

        public List<string> arr_str_list_inventory = new List<string>();
    }
}