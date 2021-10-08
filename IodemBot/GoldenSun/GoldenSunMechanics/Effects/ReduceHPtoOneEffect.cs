using System;
using System.Collections.Generic;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class ReduceHPtoOneEffect : Effect
    {
        public override string Type { get; } = "ReduceHPToOne";
        private int Probability { get; set; } = 10;

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            var log = new List<string>();
            if (Target.IsImmuneToHPtoOne)
            {
                return log;
            }

            if (Target.IsAlive)
            {
                if (Global.RandomNumber(0, 100) <= Probability)
                {
                    if (Target.GetTeam().Count > 1)
                    {
                        Target.Stats.HP = 1;
                    }
                    else
                    {
                        Target.Stats.HP = Math.Min(Target.Stats.HP, (int)(Target.Stats.MaxHP * 0.15));
                    }
                    log.Add($"<:Exclamatory:549529360604856323> {Target.Name} barely holds on.");
                }
            }
            return log;
        }

        public override string ToString()
        {
            return $"{(Probability != 100 ? $"{Probability}% chance to set" : "Set")} targets HP to one";
        }
    }
}