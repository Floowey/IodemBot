using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class AddDamageEffect : IEffect
    {
        private readonly uint addDamage = 0;

        public override string Type { get; } = "AddDamage";

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            User.addDamage += addDamage;
            return new List<string>();
        }

        public override string ToString()
        {
            return $"+{addDamage} damage";
        }
    }
}