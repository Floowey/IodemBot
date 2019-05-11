using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class PPDrainEffect : IEffect
    {
        private uint percentage = 20;
        private uint probability = 100;

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            if (Global.random.Next(0, 100) <= probability)
            {
                uint recovery = User.damageDoneThisTurn * percentage / 100;
                return User.restorePP(recovery);
            }
            return new List<string>();
        }

        public PPDrainEffect(string[] args)
        {
            if (args.Length == 1)
            {
                uint.TryParse(args[0], out percentage);
            }
            else if (args.Length == 2)
            {
                uint.TryParse(args[0], out percentage);
                uint.TryParse(args[1], out probability);
            }
        }

        public override string ToString()
        {
            return $"Restore {percentage}% of the damage done this turn in PP.";
        }
    }
}