using IodemBot.Modules.ColossoBattles;
using System;
using System.Collections.Generic;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class AddDamageEffect : IEffect
    {
        private readonly uint addDamage = 0;

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
                Console.WriteLine("Construtor for AddDamage not initialized correctly. Using default Values.");
            }
        }

        public override string ToString()
        {
            return $"+{addDamage} damage";
        }
    }
}