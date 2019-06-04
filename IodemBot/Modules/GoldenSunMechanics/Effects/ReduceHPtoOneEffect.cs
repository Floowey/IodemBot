using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class ReduceHPtoOneEffect : IEffect
    {
        private readonly int Probability = 10;

        public ReduceHPtoOneEffect(string[] args)
        {
            if (args.Length == 1)
            {
                int.TryParse(args[0], out Probability);
            }
        }

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            var log = new List<string>();
            if (Target.IsImmuneToHPtoOne)
            {
                return log;
            }

            if (Target.IsAlive())
            {
                if (Global.Random.Next(1, 100) <= Probability)
                {
                    Target.stats.HP = 1;
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