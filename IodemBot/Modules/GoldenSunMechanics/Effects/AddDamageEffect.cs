using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class AddDamageEffect : IEffect
    {
        uint addDamage = 0;
        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            User.addDamage += addDamage;
            return new List<string>();
        }

        public AddDamageEffect(string[] args)
        {
            timeToActivate = TimeToActivate.beforeDamge;
            if (args.Length == 1)
            {
                uint.TryParse(args[0], out addDamage);
            }
            else
            {
                Console.WriteLine("Construtor for MultiplyDamage not initialized correctly. Using default Values.");
            }
        }
    }   
}
