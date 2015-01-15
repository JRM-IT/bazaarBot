using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System;

namespace BazaarBot.Engine
{
    public class AgentClass
    {
        public string id;
        public int money;

        Dictionary<string, float> _ideal;
        Dictionary<string, float> _start;
        Dictionary<string, float> _size;
        
        public float max_inventory_size;
        public AgentLogic logic;

        public AgentClass(JToken data)
        {
            _ideal = new Dictionary<string, float>();
            _start = new Dictionary<string, float>();
            _size = new Dictionary<string, float>();

            if (data != null)
            {
                foreach (var property in data.OfType<JProperty>())
                {
                    switch (property.Name)
                    {
                        case ("id"):
                            {
                                id = (string)property.Value;
                                break;
                            }
                        case ("money"):
                                {
                                    money = (int)property.Value;
                                    break;
                                }
                        case ("inventory"):
                                {
                                    foreach (var child in property.Children().First().Children<JProperty>())
                                    {
                                        switch (child.Name)
                                        {
                                            case ("start"):
                                                {
                                                    Populate(child, _start);
                                                    break;
                                                }
                                            case ("ideal"):
                                                {
                                                    Populate(child, _ideal);
                                                    break;
                                                }
                                            case ("max_size"):
                                                {
                                                    max_inventory_size = (int)child.Value;
                                                    break;
                                                }
                                            default: throw new Exception("Unknown property: " + child.Name);
                                        }
                                    }
                                    break;
                                }
                        case ("logic"):
                                {
                                    logic = new AgentLogic(property.Value);
                                    break;
                                }
                        
                        default: throw new Exception("Unknown property: " + property.Name);
                    }
                }
            }
        }

        private static void Populate(JProperty child, Dictionary<string,float> dictionary)
        {
            foreach (var item in child.Children().First().Children<JProperty>())
            {
                dictionary[item.Name] = (float)item.Value;
            }
        }

        public Inventory GetStartInventory()
        {
            var i = new Inventory();
            i.ideal = new Dictionary<string, float>(_ideal);
            i.stuff = new Dictionary<string, float>(_start);
            i.sizes = new Dictionary<string, float>(_size);
            i.max_size = max_inventory_size;
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