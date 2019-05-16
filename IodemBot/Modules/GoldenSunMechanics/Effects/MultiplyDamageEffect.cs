using IodemBot.Modules.ColossoBattles;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class MultiplyDamageEffect : IEffect
    {
        private double[] multipliers = { 2.0 };
        private readonly int[] probabilites = { 10 };

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

        public MultiplyDamageEffect(string[] args)
        {
            //["1.5, 2.5", "20, 10"]
            timeToActivate = TimeToActivate.beforeDamge;
            if (args.Length == 2)
            {
                multipliers = args[0].Split(',').Select(n => Convert.ToDouble(n, new CultureInfo("en-GB"))).ToArray();
                probabilites = args[1].Split(',').Select(n => Convert.ToInt32(n)).ToArray();
            }
            else if (args.Length == 1)
            {
                multipliers = args[0].Split(',').Select(n => Convert.ToDouble(n, new CultureInfo("en-GB"))).ToArray();
                probabilites = new[] { 100 };
            }
            else
            {
                Console.WriteLine("Constructor for MultiplyDamage not initialized correctly. Using default Values.");
            }
        }

        public override string ToString()
        {
            return $"{(probabilites[0] == 100 ? "" : "Chance to do ")}{string.Join("x, ", multipliers)}x Damage";
        }
    }
}