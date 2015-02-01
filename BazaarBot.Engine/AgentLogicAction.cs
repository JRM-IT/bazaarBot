using SimpleJSON;
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
    }
}