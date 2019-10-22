using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class HPDrainEffect : Effect
    {
        private uint Percentage { get; set; } = 20;
        private uint Probability { get; set; } = 100;

        public override string Type { get; } = "HPDrain";

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            if (Global.Random.Next(0, 100) <= Probability)
            {
                uint recovery = User.damageDoneThisTurn * Percentage / 100;
                return User.Heal(recovery);
            }
            return new List<string>();
        }

        public override string ToString()
        {
            return $"{(Probability < 100 ? "Chance to restore" : "Restore")} {Percentage}% of the damage done this turn in HP";
        }
    }
}