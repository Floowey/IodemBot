using System.Collections.Generic;
using System.Linq;
using IodemBot.ColossoBattles;
using IodemBot.Extensions;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class ConditionMultEffect : Effect
    {
        public override string Type => "ConditionMult";
        public List<Condition> Conditions { get; set; }
        public double Multiplier { get; set; } = 1.5;

        public override string ToString()
        {
            return $"Deals {Multiplier}x damage when the target is afflicted with {string.Join(", ", Conditions)}";
        }

        protected override int InternalChooseBestTarget(List<ColossoFighter> targets)
        {
            var afflictedEnemies = targets.Where(s => Conditions.Any(c => s.HasCondition(c))).ToList();
            return targets.IndexOf(afflictedEnemies.Random());
        }

        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            var mult = 1.0;
            Conditions.ForEach(c => mult *= target.HasCondition(c) ? Multiplier : 1.0);
            user.OffensiveMult *= mult;
            return new List<string>();
        }
    }
}