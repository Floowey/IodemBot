using System.Collections.Generic;
using IodemBot.ColossoBattles;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class MayIgnoreDefenseEffect : Effect
    {
        public override string Type => "IgnoreDefense";
        [JsonProperty] private int Percentage { get; set; } = 20;
        [JsonProperty] private int Probability { get; set; } = 10;

        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            if (Global.RandomNumber(0, 100) <= Probability)
            {
                target.IgnoreDefense = (1 - Percentage / 100);
            }

            return new List<string>();
        }

        public override string ToString()
        {
            return $"{(Probability != 100 ? $"{Probability}% chance to ignore " : "Ignore")} {Percentage}% of Defense";
        }
    }
}