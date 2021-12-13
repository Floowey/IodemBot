using System;
using System.Collections.Generic;
using System.Linq;
using IodemBot.ColossoBattles;
using IodemBot.Extensions;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class RestoreEffect : Effect
    {
        public override string Type => "Restore";

        public static readonly Condition[] defaultConditions =
        {
            Condition.Poison,
            Condition.Venom,
            Condition.Seal,
            Condition.Sleep,
            Condition.Stun,
            Condition.DeathCurse,
            Condition.Delusion
        };

        public Condition[] CureConditions { get; set; } = Array.Empty<Condition>();
        private Condition[] TargetConditions => CureConditions.Any() ? CureConditions : defaultConditions;

        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            if (!target.IsAlive)
                return new List<string>();

            target.RemoveCondition(TargetConditions);

            if (user is PlayerFighter p)
            {
                p.BattleStats.Supported++;
            }
            return new List<string>() { $"{target.Name}'s Conditions were cured." };
        }

        public override string ToString()
        {
            return "Restore the target from Conditions and Poison";
        }

        protected override int InternalChooseBestTarget(List<ColossoFighter> targets)
        {
            var unaffectedEnemies = targets.Where(s => s.HasCurableCondition).ToList();
            return targets.IndexOf(unaffectedEnemies.Random());
        }

        protected override bool InternalValidSelection(ColossoFighter user)
        {
            return user.Party.Any(s => s.HasCondition(TargetConditions));
        }
    }
}