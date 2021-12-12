using System.Collections.Generic;
using IodemBot.ColossoBattles;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class HpDrainEffect : Effect
    {
        [JsonProperty] private uint Percentage { get; set; } = 20;
        [JsonProperty] private uint Probability { get; set; } = 100;

        public override string Type => "HPDrain";

        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            if (Global.RandomNumber(0, 100) <= Probability)
            {
                uint recovery = user.DamageDoneThisTurn * Percentage / 100;
                return user.Heal(recovery);
            }
            return new List<string>();
        }

        public override string ToString()
        {
            return $"{(Probability < 100 ? $"{Probability}% chance to restore" : "Restore")} {Percentage}% of the damage done this turn in HP";
        }
    }
}