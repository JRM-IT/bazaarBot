using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BazaarBot.WpfApp
{
    public class ChangeViewModel : ViewModel
    {
        public List<string> Commodities { get; set; }

        public List<string> AgentClasses { get; set; }

        public int Amount { get; set; } 

        public ChangeViewModel(BazaarBot.Engine.BazaarBot bazaar)
        {
            Commodities = bazaar.CommodityClasses;

            AgentClasses = bazaar.AgentClasses.Keys.ToList();

            Amount = 10;
        }

        public string SelectedAgentClass { get; set; }

        public string SelectedCommodity { get; set; }
    }
}
