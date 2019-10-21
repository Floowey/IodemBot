using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class StatEffect : IEffect
    {
        public override string Type { get; } = "Stat";
        private string Stat { get; set; }
        private double Multiplier { get; set; } = 1;
        private int Probability { get; set; } = 100;
        private bool OnTarget { get; set; } = true;
        private int Turns { get; set; } = 7;

        public override string ToString()
        {
            return $"{(Probability == 100 ? $"{(Multiplier > 1 ? "Raise" : "Lower")}" : $"{Probability}% chance to {(Multiplier > 1 ? "raise" : "lower")}")} {Stat} of {(OnTarget ? "target" : "user")} to {Multiplier * 100}%";
        }

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            List<string> log = new List<string>();
            if (!Target.IsAlive)
            {
                return log;
            }

            if (Global.Random.Next(1, 100) <= Probability)
            {
                if (OnTarget)
                {
                    Target.ApplyBuff(new Buff(Stat, Multiplier, (uint)Turns));
                    log.Add($"{Target.Name}'s {Stat} {(Multiplier > 1 ? "rises" : "lowers")}.");
                }
                else
                {
                    User.ApplyBuff(new Buff(Stat, Multiplier, (uint)Turns));
                    log.Add($"{User.Name}'s {Stat} {(Multiplier > 1 ? "rises" : "lowers")}.");
                }
            }

            return log;
        }
    }
}