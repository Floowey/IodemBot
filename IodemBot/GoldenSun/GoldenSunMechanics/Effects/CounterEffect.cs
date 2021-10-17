using System.Collections.Generic;
using IodemBot.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class CounterEffect : Effect
    {
        public override string Type { get; } = "Counter";

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