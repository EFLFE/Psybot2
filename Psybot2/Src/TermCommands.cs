using System;

namespace Psybot2.Src
{
    internal sealed class TermCommands
    {
        private string trigger;
        private string info;
        private Action act;
        private Func<bool> condition;

        public string GetTrigger => trigger;

        public string GetInfo => info;

        public TermCommands(string trigger, string info, Action act, Func<bool> condition = null)
        {
            this.trigger = trigger;
            this.info = info;
            this.act = act;
            this.condition = condition;
        }

        public bool Excecute(ref string _trigger)
        {
            if (trigger.Equals(_trigger, StringComparison.InvariantCultureIgnoreCase))
            {
                if (condition != null && !condition())
                {
                    return true;
                }
                act();
                return true;
            }
            return false;
        }

    }
}
