using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class MultiplyDamageEffect : Effect
    {
        public override string Type { get; } = "MultiplyDamage";
        private double[] Multipliers { get; set; } = { 2.0 };
        private int[] Probabilites { get; set; } = { 10 };

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            for (int i = 0; i < Multipliers.Length; i++)
            {
                if (Global.Random.Next(0, 100) <= Probabilites[i])
                {
                    User.offensiveMult *= Multipliers[i];
                    break;
                }
            }
            return new List<string>();
        }

        public override string ToString()
        {
            return $"{(Probabilites[0] == 100 ? "" : "Chance to do ")}{string.Join("x, ", Multipliers)}x Damage";
        }
    }
}