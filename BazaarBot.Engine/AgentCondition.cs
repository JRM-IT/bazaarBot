namespace BazaarBot.Engine
{
    public class AgentCondition
    {
        public string Condition { get; set; }
        public bool Negated { get; set; }

        public AgentCondition(string condition)
        {
            if (condition.Substring(0, 1) == "!")
            {
                Negated = true;
                condition = condition.Substring(1);
            }
            Condition = condition;
        }
    }
}