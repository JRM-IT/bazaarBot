using Newtonsoft.Json.Linq;
using System;
namespace BazaarBot.Engine
{
    public class AgentLogic
    {
        string source;
        AgentLogicNode root;

        public AgentLogic(JToken data)
        {
            source = data.ToString();
            root = new AgentLogicNode(data);
        }

        public float GetProduction(string commodity, AgentLogicNode currentNode = null)
        {
            if (currentNode == null)
                return GetProduction(commodity, root);
            else
            {
                if (!currentNode.IsLeaf)
                    return GetProduction(commodity, currentNode.NodeTrue) + GetProduction(commodity, currentNode.NodeFalse);
                else
                {
                    var amount = 0f;
                    foreach (var act in currentNode.Actions)
                    {
                        for (int i = 0; i < act.Targets.Length; i++)
                        {
                            switch (act.action)
                            {
                                case "produce":
                                    if (act.Targets[i] == commodity)
                                        amount += act.Chances[i] * act.amounts[i];
                                    break;
                                case "transform":
                                    var amt = act.amounts[i];
                                    if (amt == -1) { amt = 1; }			//can be misleading
                                    if (act.results[i] == commodity)
                                    {
                                        amount += act.Chances[i] * amt * act.efficiency[i];
                                    }
                                    break;
                            }
                        }
                    }
                    return amount;
                }
            }
        }

        public void Perform(Agent agent)
        {
            Perform(root, agent);
        }

        private void Perform(AgentLogicNode currentNode, Agent agent)
        {
            if (!currentNode.IsLeaf)
            {
                if (Evaluate(currentNode, agent))
                {
                    if (currentNode.NodeTrue != null)
                        Perform(currentNode.NodeTrue, agent);
                }
                else
                {
                    if (currentNode.NodeFalse != null)
                        Perform(currentNode.NodeFalse, agent);
                }
            }
            else
            {												//Do the actions
                foreach (var act in currentNode.Actions)
                {
                    for (int i = 0; i < act.Targets.Length; i++)
                    {
                        var amount = act.amounts[i];
                        var target = act.Targets[i];
                        var chance = act.Chances[i];

                        //Roll to see if this happens
                        if (chance >= 1.0 || RNG.Next() < chance)
                        {

                            float curr_amount = agent.QueryInventory(target);

                            if (amount == -1) //-1 means "match my total value"
                                amount = curr_amount;

                            switch (act.action)
                            {
                                case "produce":
                                    agent.ChangeInventory(target, amount); //produce some stuff
                                    break;
                                case "consume":
                                    agent.ChangeInventory(target, -amount); //consume some stuff
                                    break;
                                case "transform":
                                    float amount_target = amount;

                                    //exchange rate between A -> B								
                                    float amount_product = amount * act.efficiency[i];
                                    string result = act.results[i];

                                    agent.ChangeInventory(target, -amount_target);	//consume this much of A
                                    agent.ChangeInventory(result, amount_product); //produce this much of B	
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private bool Evaluate(AgentLogicNode currentNode, Agent agent)
        {
            //Fail on the first condition that is false
            foreach (var c in currentNode.Conditions)
            {
                switch (c.Condition)
                {
                    case "has":	//Do you have something?
                        var has = false;
                        foreach (var str in currentNode.Parameters.Values<string>())
                        {		//look at all the things
                            float amount = agent.QueryInventory(str.ToString());	//count em
                            if (amount > 0)
                                has = true;					//have it or not
                            if (c.Negated)
                            {					//looking for NO-has
                                if (has) { return false; }
                            }
                            else
                            {								//looking for YES-has
                                if (!has) { return false; }
                            }
                        }
                        break;
                }
            }
            return true;
        }
    }
}