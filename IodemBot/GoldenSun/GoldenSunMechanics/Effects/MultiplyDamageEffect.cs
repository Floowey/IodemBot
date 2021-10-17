using System.Collections.Generic;
using IodemBot.ColossoBattles;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class MultiplyDamageEffect : Effect
    {
        public override string Type { get; } = "MultiplyDamage";
        [JsonProperty] private double[] Multipliers { get; set; } = { 2.0 };
        [JsonProperty] private int[] Probabilities { get; set; } = { 10 };

        public MultiplyDamageEffect()
        {
            ActivationTime = TimeToActivate.beforeDamage;
        }

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            for (int i = 0; i < Multipliers.Length; i++)
            {
                if (Global.RandomNumber(0, 100) <= Probabilities[i])
                {
                    User.offensiveMult *= Multipliers[i];
                    return new List<string>();
                }
            }
            return new List<string>();
        }

        public override string ToString()
        {
            return $"{(Probabilities[0] == 100 ? "" : $"Chance to do ")}{string.Join("x, ", Multipliers)}x Damage";
        }
    }
}