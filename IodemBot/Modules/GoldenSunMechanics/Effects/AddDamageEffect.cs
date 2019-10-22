using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class AddDamageEffect : Effect
    {
        public override string Type { get; } = "AddDamage";

        public uint AddDamage { get; set; } = 0;

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            User.addDamage += AddDamage;
            return new List<string>();
        }

        public override string ToString()
        {
            return $"+{AddDamage} damage";
        }
    }
}