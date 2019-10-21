using IodemBot.Extensions;
using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class ReduceDamageEffect : IEffect
    {
        public override string Type { get; } = "ReduceDamage";
        private int damageReduction { get; set; } = 0;

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            List<string> log = new List<string>();
            if (!Target.IsAlive)
            {
                return log;
            }

            Target.defensiveMult *= (double)(100 - damageReduction) / 100;

            return log;
        }

        protected override int InternalChooseBestTarget(List<ColossoFighter> targets)
        {
            var target = targets.Where(d => d.Name.Contains("Star")).FirstOrDefault() ?? targets.Random();
            return targets.IndexOf(target);
        }

        public override string ToString()
        {
            return $"Reduces damage taken by {damageReduction}%";
        }
    }
}