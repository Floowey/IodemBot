using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class MayIgnoreDefenseEffect : Effect
    {
        public override string Type { get; } = "IgnoreDefense";
        private int IgnorePercent { get; set; } = 20;
        private int Probability { get; set; } = 10;

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