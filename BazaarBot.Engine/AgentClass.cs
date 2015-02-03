using System.Collections.Generic;
using System.Linq;
using System;
using SimpleJSON;

namespace BazaarBot.Engine
{
    public class AgentClass
    {
        public string id;
        public int money;
        public AgentLogic logic;
        
        Dictionary<string, float> _ideal;
        Dictionary<string, float> _start;
        Dictionary<string, float> _size;
        float _maxInventorySize;
        private Dictionary<string, float> ideal;
        private Dictionary<string, float> start;
        private int maxInventorySize;

        public AgentClass(Dictionary<string, float> ideal, Dictionary<string, float> start, int maxInventorySize)
        {
            _ideal = ideal;
            _start = start;
            _maxInventorySize = maxInventorySize;
            _size = new Dictionary<string, float>();
        }
        
        public Inventory GetStartInventory()
        {
            var i = new Inventory();
            i.ideal = new Dictionary<string, float>(_ideal);
            i.stuff = new Dictionary<string, float>(_start);
            i.sizes = new Dictionary<string, float>(_size); // is never populated, will always be empty
            i.max_size = _maxInventorySize;
            return i;
        }

        public Dictionary<string, float> Convert(IList<string> ids, IList<float> amounts)
        {
            var dictionary = new Dictionary<string, float>();

            for (int i = 0; i < ids.Count; i++)
                dictionary.Add(ids[i], amounts[i]);

            return dictionary;
        }
    }
}