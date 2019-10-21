using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class HPDrainEffect : IEffect
    {
        private uint percentage { get; set; } = 20;
        private uint probability { get; set; } = 100;

        public override string Type { get; } = "HPDrain";

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            if (Global.Random.Next(0, 100) <= probability)
            {
                uint recovery = User.damageDoneThisTurn * percentage / 100;
                return User.Heal(recovery);
            }
            return new List<string>();
        }

        public override string ToString()
        {
            return $"{(probability < 100 ? "Chance to restore" : "Restore")} {percentage}% of the damage done this turn in HP";
        }
    }
}