using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class MultiplyDamgeEffect : IEffect
    {
        double[] multipliers = {2.0 };
        int[] probabilites = { 10 };
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

        public MultiplyDamgeEffect(object[] args)
        {
            if(args.Length == 2 && args[0] is double[] && args[1] is int[])
            {
                multipliers = (double[])args[0];
                probabilites = (int[])args[1];
            } else
            {
                Console.WriteLine("Construtor for MultiplyDamage not initialized correctly. Using default Values.");
            }
        }
    }
}
