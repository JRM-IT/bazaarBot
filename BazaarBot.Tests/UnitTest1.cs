using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.IO;
using BazaarBot.Engine;

namespace BazaarBot.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void AgentLogicNodeTest()
        {
            var json = JObject.Parse(File.ReadAllText("settings.json"));
            var token = json.SelectToken("agents[0].logic");
            var logic = new AgentLogicNode(token);
        }

        [TestMethod]
        public void AgentClassTest()
        {
            var json = JObject.Parse(File.ReadAllText("settings.json"));
            var token = json.SelectToken("agents[0]");
            var logic = new AgentClass(token);
        }

        [TestMethod]
        public void BazaarBotTest()
        {
            var bazaar = new Engine.BazaarBot(new StandardRandomNumberGenerator(0));
            bazaar.LoadJsonSettings("settings.json");
        }
    }
}
