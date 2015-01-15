namespace BazaarBot.Engine
{
    public class Offer
    {
        public string Commodity { get; private set; } 	//the thing offered
        public float Units { get;  private set; }		//how many units
        public float UnitPrice { get; private set; }	//price per unit
        public int AgentId { get; private set; }    	//who offered this

        public Offer(int agentId, string commodity, float units, float unitPrice)
        {
            AgentId = agentId;
            Commodity = commodity;
            Units = units;
            UnitPrice = unitPrice;
        }

        public void Trade(float quantity)
        {
            Units -= quantity;
        }

        public override string ToString()
        {
            return string.Format("({0}): {1}x{2} @ {3}", AgentId, Commodity, Units, UnitPrice);
        }
    }
}
