using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class ReduceDamageEffect : IEffect
    {
        private readonly int damageReduction = 0;

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            List<string> log = new List<string>();
            if (!Target.IsAlive())
            {
                return log;
            }

            Target.defensiveMult *= (double)(100 - damageReduction) / 100;

            return log;
        }

        public ReduceDamageEffect(string[] args)
        {
            if (args.Length == 1)
            {
                int.TryParse(args[0], out damageReduction);
            }
        }

        public override string ToString()
        {
            return $"Reduces damage taken by {damageReduction}%";
        }
    }
}