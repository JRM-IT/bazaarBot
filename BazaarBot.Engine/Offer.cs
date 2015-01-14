namespace BazaarBot.Engine
{

    public class Offer
    {
        public string Commodity;	//the thing offered
        public float Units;			//how many units
        public float UnitPrice;	//price per unit
        public int AgentId;    	//who offered this

        public Offer(int agentId = -1, string commodity = "", float units = 1, float unitPrice = 1)
        {
            AgentId = agentId;
            Commodity = commodity;
            Units = units;
            UnitPrice = unitPrice;
        }
        public override string ToString()
        {
            return string.Format("({0}): {1}x{2} @ {3}", AgentId, Commodity, Units, UnitPrice);
        }
    }
}
