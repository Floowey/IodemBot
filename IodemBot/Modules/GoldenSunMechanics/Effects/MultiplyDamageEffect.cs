using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class MultiplyDamageEffect : IEffect
    {
        public override string Type { get; } = "MultiplyDamage";
        private double[] multipliers { get; set; } = { 2.0 };
        private int[] probabilites { get; set; } = { 10 };

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            for (int i = 0; i < multipliers.Length; i++)
            {
                if (Global.Random.Next(0, 100) <= probabilites[i])
                {
                    User.offensiveMult *= multipliers[i];
                    break;
                }
            }
            return new List<string>();
        }

        public override string ToString()
        {
            return $"{(probabilites[0] == 100 ? "" : "Chance to do ")}{string.Join("x, ", multipliers)}x Damage";
        }
    }
}