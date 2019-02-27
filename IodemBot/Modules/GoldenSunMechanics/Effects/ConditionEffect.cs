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
            init(stringCondition, probability);
        }

        public ConditionEffect(params object[] args)
        {
            if(args.Length != 2)
            {
                if (args[0] is string && args[1] is int)
                {
                    init((string)args[0], (int)args[1]);
                }
            } else
            {
                throw new ArgumentException("Condition, probability");
            }
        }

        private void init(string stringCondition, int probability)
        {
            Probability = probability;
            if (!Enum.TryParse<Condition>(stringCondition, out Cond))
            {
                throw new ArgumentException("stringCondition");
            }
        }

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            List<string> log = new List<string>();
            if (Target.isImmuneToEffects) return log;
            if (!Target.IsAlive()) return log;
            if(Global.random.Next(1, 100) <= Probability)
            {
                Target.AddCondition(Cond);
                log.Add($"{Target.name} gets hit with {Cond.ToString()}!");
            }
            return log;
        }
    }
}
