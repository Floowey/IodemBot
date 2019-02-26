using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class ConditionEffect : IEffect
    {
        private Condition Cond;
        private int Probability;

        public ConditionEffect(string stringCondition, int probability)
        {
            if(Enum.TryParse<Condition>(stringCondition, out Cond))
            Probability = probability;
        }

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            List<string> log = new List<string>();
            if(Global.random.Next(0, 100) <= Probability)
            {
                Target.AddCondition(Cond);
                log.Add($"{Target.name} gets hit with {Cond.ToString()}!");
            }
            return log;
        }
    }
}
