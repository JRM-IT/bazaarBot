using SimpleJSON;
using System.Collections.Generic;
using System.Linq;

namespace BazaarBot.Engine
{
    public class AgentLogicNode
    {
        public bool IsLeaf;		//if it's a leaf node, it should only have actions
                                //if it's a branch node, it should only have conditions/params

        public AgentCondition[] Conditions;
        public string[] Parameters;

        public AgentLogicNode NodeTrue;
        public AgentLogicNode NodeFalse;
        public List<AgentLogicAction> Actions;

        public override string ToString()
        {
            return string.Join(",", Conditions.Select(p => p.ToString()).ToArray()) + " " + string.Join(",", Parameters);
        }
    }
}