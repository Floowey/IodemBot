using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class CounterEffect : IEffect
    {
        public CounterEffect()
        {
        }

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            Target.AddCondition(Condition.Counter);
            return new List<string>() { $"{Target.name} gets ready to strike back!" };
        }

        public override string ToString()
        {
            return "Puts the target in the Counter State";
        }
    }
}
