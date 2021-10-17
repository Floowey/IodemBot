﻿using System.Collections.Generic;
using IodemBot.ColossoBattles;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class HPDrainEffect : Effect
    {
        [JsonProperty] private uint Percentage { get; set; } = 20;
        [JsonProperty] private uint Probability { get; set; } = 100;

        public override string Type { get; } = "HPDrain";

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            if (Global.RandomNumber(0, 100) <= Probability)
            {
                uint recovery = User.damageDoneThisTurn * Percentage / 100;
                return User.Heal(recovery);
            }
            return new List<string>();
        }

        public override string ToString()
        {
            return $"{(Probability < 100 ? $"{Probability}% chance to restore" : "Restore")} {Percentage}% of the damage done this turn in HP";
        }
    }
}