using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class ReduceHPtoOneEffect : IEffect
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
                if (Global.Random.Next(1, 100) <= Probability)
                {
                    Target.Stats.HP = 1;
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