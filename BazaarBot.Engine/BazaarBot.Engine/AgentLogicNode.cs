using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace BazaarBot.Engine
{
    public class AgentLogicNode
    {
        public bool IsLeaf;		//if it's a leaf node, it should only have actions
                                //if it's a branch node, it should only have conditions/params

        public List<AgentCondition> Conditions;
        public JToken[] Parameters;

        public AgentLogicNode NodeTrue;
        public AgentLogicNode NodeFalse;
        public List<AgentLogicAction> Actions;

        public AgentLogicNode(JToken data)
        {
            if (data != null)
            {
                var properties = data.OfType<JProperty>();
                if (properties.Any(p => p.Name == "condition"))
                {
                    IsLeaf = false;
                    Conditions = properties.Single(p => p.Name == "condition").Value.Values<string>().Select(p => new AgentCondition(p)).ToList();
                    Parameters = properties.Single(p => p.Name == "param").Value.Values().ToArray();
                    NodeTrue = GetLogicNode(properties, "if_true");
                    NodeFalse = GetLogicNode(properties, "if_false");
                }
                else
                {
                    IsLeaf = true;
                    Actions = properties.Single(p => p.Name == "action").Value.Select(p => new AgentLogicAction(p)).ToList();
                }
            }
        }

        private AgentLogicNode GetLogicNode(IEnumerable<JProperty> properties, string propertyName)
        {
            var nodeTrue = properties.SingleOrDefault(p => p.Name == propertyName);
            if (nodeTrue != null)
                return new AgentLogicNode(nodeTrue.Value);
            return null;
        }
    }
}