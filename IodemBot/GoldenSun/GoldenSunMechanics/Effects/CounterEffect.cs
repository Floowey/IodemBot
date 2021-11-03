using System.Collections.Generic;
using IodemBot.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class CounterEffect : Effect
    {
        public override string Type => "Counter";

        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            target.AddCondition(Condition.Counter);
            return new List<string>() { $"{target.Name} gets ready to strike back!" };
        }

        public override string ToString()
        {
            return "Puts the target in the Counter State";
        }
    }
}