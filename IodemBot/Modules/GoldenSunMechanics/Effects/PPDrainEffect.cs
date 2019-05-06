using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class PPDrainEffect : IEffect
    {
        private uint percentage = 10;

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            uint recovery = User.damageDoneThisTurn * percentage / 100;
            return User.heal(recovery);
        }

        public PPDrainEffect(string[] args)
        {
            if (args.Length == 1)
            {
                uint.TryParse(args[0], out percentage);
            }
        }

        public override string ToString()
        {
            return $"Restore {percentage}% of the damage done this turn in PP.";
        }
    }
}