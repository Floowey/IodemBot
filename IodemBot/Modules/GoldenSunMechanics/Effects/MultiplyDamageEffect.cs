using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class MultiplyDamageEffect : IEffect
    {
        private double[] multipliers = {2.0 };
        private int[] probabilites = { 10 };
        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            for (int i = 1; i < multipliers.Length; i++){
                if(Global.random.Next(1,100) <= probabilites[i])
                {
                    User.offensiveMult = multipliers[i];
                    break;
                }
            }
            return new List<string>();
        }

        public MultiplyDamageEffect(string[] args)
        {
            //["1.5, 2.5", "20, 10"]
            timeToActivate = TimeToActivate.beforeDamge;
            if(args.Length == 2)
            {
                multipliers = args[0].Split(',').Select(n => Convert.ToDouble(n)).ToArray();
                probabilites = args[1].Split(',').Select(n => Convert.ToInt32(n)).ToArray();
            } else
            {
                Console.WriteLine("Construtor for MultiplyDamage not initialized correctly. Using default Values.");
            }
        }

        public override string ToString()
        {
            return $"Chance to do {string.Join("x,", multipliers)}x Damage";
        }
    }
}
