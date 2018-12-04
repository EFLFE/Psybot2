using System;

namespace Psybot2.Src
{
    internal sealed class TermCommands
    {
        private string trigger;
        private string info;
        private Action<string[]> act;
        private Func<bool> condition;

        public string GetTrigger => trigger;

        public string GetInfo => info;

        public TermCommands(string trigger, string info, Action<string[]> act, Func<bool> condition = null)
        {
            this.trigger = trigger;
            this.info = info;
            this.act = act;
            this.condition = condition;
        }

        public bool Excecute(string[] args)
        {
            if (trigger.Equals(args[0], StringComparison.InvariantCultureIgnoreCase))
            {
                if (condition != null && !condition())
                {
                    return true;
                }
                act(args);
                return true;
            }
            return false;
        }

    }
}
