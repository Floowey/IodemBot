using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;

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
            return new List<string>() { $"{Target.Name} gets ready to strike back!" };
        }

        public override string ToString()
        {
            return "Puts the target in the Counter State";
        }
    }
}