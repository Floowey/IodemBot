using System.Collections.Generic;
using System.Linq;
using IodemBot.ColossoBattles;
using IodemBot.Extensions;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class ConditionEffect : Effect
    {
        public override string Type { get; } = "Condition";
        public Condition Condition { get; set; }
        public int Probability { get; set; } = 10;

        public override string ToString()
        {
            return $"{(Probability != 100 ? $"{Probability}% chance to apply" : "Apply")} {Condition}";
        }

        protected override int InternalChooseBestTarget(List<ColossoFighter> targets)
        {
            var unaffectedEnemies = targets.Where(s => !s.HasCondition(Condition)).ToList();
            return targets.IndexOf(unaffectedEnemies.Random());
        }

        protected override bool InternalValidSelection(ColossoFighter user)
        {
            return !user.Enemies.All(s => s.HasCondition(Condition));
        }

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            List<string> log = new List<string>();
            if (Target.isImmuneToConditions.Contains(Condition))
            {
                return log;
            }

            if (!Target.IsAlive)
            {
                return log;
            }

            if (Global.RandomNumber(0, 100) <= Probability)
            {
                Target.AddCondition(Condition);
                log.Add($"{Target.Name} gets hit with {Condition}!");
            }
            return log;
        }
    }
}