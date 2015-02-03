using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using BazaarBot.Engine;
using SimpleJSON;

namespace BazaarBot.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void AgentLogicNodeTest()
        {
            var json = JSON.Parse(File.ReadAllText("settings.json"));
            var node = json["agents"][0]["logic"];
            JSONParser.ParseAgentLogicNode(node); 
        }

        [TestMethod]
        public void AgentClassTest()
        {
            var json = JSON.Parse(File.ReadAllText("settings.json"));
            var node = json["agents"][0];
            JSONParser.ParseAgentClass(node);
        }

        [TestMethod]
        public void BazaarBotTest()
        {
            var bazaar = new Engine.BazaarBot(new StandardRandomNumberGenerator(0));
            JSONParser.LoadJsonSettings(bazaar, "settings.json");
        }
    }
}
