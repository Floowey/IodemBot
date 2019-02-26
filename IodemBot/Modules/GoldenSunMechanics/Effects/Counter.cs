using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics.Effects
{
    public class Counter : IEffect
    {
        public Counter(params object[] args)
        {
        }

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            Target.AddCondition(Condition.Counter);
            return new List<string>() { $"{Target.name} gets ready to strike back!" };
        }
    }
}
