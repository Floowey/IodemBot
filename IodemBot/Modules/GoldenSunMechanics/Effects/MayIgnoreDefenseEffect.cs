using System.Collections.Generic;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class MayIgnoreDefenseEffect : Effect
    {
        public override string Type { get; } = "IgnoreDefense";
        public int IgnorePercent { get; set; } = 20;
        public int Probability { get; set; } = 10;

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            if (Global.Random.Next(1, 100) <= Probability)
            {
                Target.ignoreDefense = (1 - IgnorePercent / 100);
            }

            return new List<string>();
        }

        public override string ToString()
        {
            return $"{(Probability != 100 ? $"{Probability}% chance to ignore " : "Ignore")} {IgnorePercent}% of Defense";
        }
    }
}