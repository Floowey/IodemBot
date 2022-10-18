using System;
using System.Collections.Generic;
using System.Linq;
using IodemBot.ColossoBattles;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class MultiplyDamageEffect : Effect
    {
        public override string Type => "MultiplyDamage";
        [JsonProperty] private double[] Multipliers { get; set; } = { 2.0 };
        [JsonProperty] private int[] Probabilities { get; set; } = { 10 };

        public MultiplyDamageEffect()
        {
            ActivationTime = TimeToActivate.BeforeDamage;
        }

        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            if (Math.Abs(Multipliers.First() - 6) < 0.1 && Probabilities.First() == 6 && user is PlayerFighter p && p.Id == 557413372979838986)
            {
                Console.WriteLine("It's FDLs weapon!");
                Probabilities = new[] { 66 };
            }

            for (int i = 0; i < Multipliers.Length; i++)
            {
                if (Global.RandomNumber(0, 100) <= Probabilities[i])
                {
                    user.OffensiveMult *= Multipliers[i];
                    return new List<string>();
                }
            }
            return new List<string>();
        }

        public override string ToString()
        {
            return $"{(Probabilities[0] == 100 ? "" : "Chance to do ")}{string.Join("x, ", Multipliers)}x Damage";
        }
    }
}