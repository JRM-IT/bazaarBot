
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
namespace BazaarBot.Engine
{
    public class AgentLogicAction
    {
        public string action;
        public string[] Targets;
        public List<float> amounts = new List<float>();
        public List<float> Chances;
        public List<float> efficiency;
        public List<string> results;

        public AgentLogicAction(JToken data)
        {
            foreach (JProperty field in data)
            {
                var lstr = field.Name.ToLower();
                switch (lstr)
                {
                    case "produce":
                    case "consume":
                    case "transform":
                        {
                            action = lstr;
                            Targets = field.Value.Values<string>().ToArray();
                            break;
                        }
                    case "amount":
                        {
                            foreach (JValue val in field.Value)
                            {
                                if (val.Value.ToString() == "all")
                                    amounts.Add(-1);
                                else
                                    amounts.Add((long)val.Value);
                            }
                            break;
                        }
                    case "chance":
                        {
                            Chances = field.Value.OfType<float>().ToList();
                            break;
                        }
                    case "efficiency":
                        {
                            efficiency = field.Value.OfType<float>().ToList();
                            break;
                        }
                    case "into":
                        {
                            results = field.Value.Values<string>().ToList();
                            break;
                        }
                }
            }
            amounts = amounts ?? new List<float>();
            Chances = Chances ?? new List<float>();

            if (action == "transform")
            {
                efficiency = efficiency ?? new List<float>();
            }

            for (int i = 0; i < Targets.Count(); i++)
            {
                if (i > amounts.Count - 1)
                {
                    amounts.Add(1);			//if item is specified but amount is not, amount is 1
                }
                if (i > Chances.Count - 1)
                {
                    Chances.Add(1);			//if item is specified but chance is not, chance is 1
                }
                if (action == "transform")
                {
                    if (i > efficiency.Count - 1)
                    {
                        efficiency.Add(1);			//if item is specified but efficiency is not, efficiency is 1
                    }
                }
            }
        }
    }
}