using System.Collections.Generic;
using IodemBot.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class AddDamageEffect : Effect
    {
        public override string Type => "AddDamage";

        public uint AddDamage { get; set; } = 0;

        public AddDamageEffect()
        {
            ActivationTime = TimeToActivate.BeforeDamage;
        }

        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            user.AddDamage += AddDamage;
            return new List<string>();
        }

        public override string ToString()
        {
            return $"+{AddDamage} damage";
        }
    }
}