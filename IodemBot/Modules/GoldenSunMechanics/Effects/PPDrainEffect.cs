using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class PPDrainEffect : IEffect
    {
        private readonly uint percentage = 20;
        private readonly uint probability = 100;

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            if (Global.Random.Next(0, 100) <= probability)
            {
                uint recovery = User.damageDoneThisTurn * percentage / 100;
                return User.RestorePP(recovery);
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
            return $"{(probability < 100 ? "Chance to restore" : "Restore")} {percentage}% of the damage done this turn in PP";
        }
    }
}