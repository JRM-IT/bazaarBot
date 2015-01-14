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
        public List<string> inventory_ideal_ids;
        public List<float> inventory_ideal_amounts;
        public List<string> inventory_start_ids;
        public List<float> inventory_start_amounts;
        public List<string> inventory_size_ids;
        public List<float> inventory_size_amounts;

        public float max_inventory_size;
        public AgentLogic logic;

        public AgentClass(JToken data)
        {
            inventory_ideal_ids = new List<string>();
            inventory_ideal_amounts = new List<float>();
            inventory_start_ids = new List<string>();
            inventory_start_amounts = new List<float>();
            inventory_size_ids = new List<string>();
            inventory_size_amounts = new List<float>();

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
                                                    Populate(child, inventory_start_ids, inventory_start_amounts);
                                                    break;
                                                }
                                            case ("ideal"):
                                                {
                                                    Populate(child, inventory_ideal_ids, inventory_ideal_amounts);
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

        private static void Populate(JProperty child, List<string> ids, List<float> amounts)
        {
            foreach (var item in child.Children().First().Children<JProperty>())
            {
                ids.Add(item.Name);
                amounts.Add((float)item.Value);
            }
        }

        public Inventory GetStartInventory()
        {
            var i = new Inventory();
            i.ideal = Convert(inventory_ideal_ids, inventory_ideal_amounts);
            i.stuff = Convert(inventory_start_ids, inventory_start_amounts);
            i.sizes = Convert(inventory_size_ids, inventory_size_amounts);
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