using System.Collections.Generic;
using System.Linq;

namespace BazaarBot.Engine
{
    public class Inventory
    {
        public float max_size;
        public Dictionary<string, float> sizes = new Dictionary<string, float>();
        public Dictionary<string, float> stuff = new Dictionary<string, float>();
        public Dictionary<string, float> ideal = new Dictionary<string, float>();

        public Inventory Copy()
        {
            return new Inventory
            {
                stuff = new Dictionary<string, float>(stuff),
                sizes = new Dictionary<string, float>(sizes),
                ideal = new Dictionary<string, float>(ideal),
                max_size = max_size
            };
        }

        public float Query(string commodityName)
        {
            return stuff.ContainsKey(commodityName) ? stuff[commodityName] : 0f;
        }

        public float Ideal(string commodityName)
        {
            return ideal.ContainsKey(commodityName) ? ideal[commodityName] : 0f;
        }

        public float Size(string commodityName)
        {
            return sizes.ContainsKey(commodityName) ? sizes[commodityName] : -1f;
        }

        public float SpaceEmpty(string commodityName)
        {
            return max_size - SpaceUsed(commodityName);
        }

        public float SpaceUsed(string commodityName)
        {
            if (!stuff.ContainsKey(commodityName))
                return 0;
            return stuff[commodityName];
        }

        public void Change(string commodity, float delta)
        {
            if (stuff.ContainsKey(commodity))
            {
                stuff[commodity] += delta;
                if (stuff[commodity] < 0)
                    stuff[commodity] = 0;
            }
            else if (delta >= 0)
                stuff[commodity] = delta;
            else
                throw new System.InvalidOperationException("Cannot remove non-existing commodity");
        }

        public float Surplus(string commodity)
        {
            var amount = Query(commodity);
            var ideal = Ideal(commodity);
            if (amount > ideal)
                return amount - ideal;
            return 0;
        }

        public float Shortage(string commodity)
        {
            var amount = Query(commodity);
            var ideal = Ideal(commodity);
            if (amount < ideal)
                return ideal - amount;
            return 0;
        }
    }
}