using System;
using System.Collections.Generic;
using IodemBot.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class ReduceHPtoOneEffect : Effect
    {
        public override string Type => "ReduceHPToOne";
        private int Probability { get; set; } = 10;

        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            var log = new List<string>();
            if (target.IsImmuneToHPtoOne)
            {
                return log;
            }

            if (target.IsAlive)
            {
                if (Global.RandomNumber(0, 100) > Probability) return log;
                target.Stats.HP = target.Party.Count > 1 ? 1 : Math.Min(target.Stats.HP, (int)(0.15 * target.Stats.MaxHP));
                log.Add($"<:Exclamatory:549529360604856323> {target.Name} barely holds on.");
            }
            return log;
        }

        public override string ToString()
        {
            return $"{(Probability != 100 ? $"{Probability}% chance to set" : "Set")} targets HP to one";
        }
    }
}