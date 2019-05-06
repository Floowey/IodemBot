using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class HPDrainEffect : IEffect
    {
        private uint percentage = 20;

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            uint recovery = User.damageDoneThisTurn * percentage / 100;
            return User.heal(recovery);
        }

        public HPDrainEffect(string[] args)
        {
            if (args.Length == 1)
            {
                uint.TryParse(args[0], out percentage);
            }
        }

        public override string ToString()
        {
            return $"Restore {percentage}% of the damage done this turn in HP.";
        }
    }
}