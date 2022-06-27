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

            var removed = target.RemoveCondition(TargetConditions);
            var easedPoison = false;
            if (target.HasCondition(Condition.Venom) && TargetConditions.Contains(Condition.Poison))
            {
                target.RemoveCondition(Condition.Venom);
                target.AddCondition(Condition.Poison);
                easedPoison = true;
                removed++;
            }

            if (user is PlayerFighter p)
            {
                p.BattleStats.Supported++;
            }
            return new List<string>() { $"{target.Name} was cured of {removed} Condition{(removed == 1 ? "" : "s")}.{(easedPoison ? $" {target.Name}'s envenomation was eased." : "")}" };
        }

        public override string ToString()
        {
            return !CureConditions.Any() ? "Restore the target from Conditions and Poison" : $"Restores the target from the following conditions:\n{string.Join(", ", TargetConditions.OrderBy(c => (int)c).Select(c => $"{Emotes.GetIcon(c, "")} {c}"))}" +
                $"{(TargetConditions.Contains(Condition.Poison) && !TargetConditions.Contains(Condition.Venom) ? $"\nCan ease {Emotes.GetIcon(Condition.Venom)} envenomation." : "")}";
        }

        protected override int InternalChooseBestTarget(List<ColossoFighter> targets)
        {
            var unaffectedEnemies = targets.Where(s => s.HasCondition(TargetConditions) ||
            (s.HasCondition(Condition.Venom) && TargetConditions.Contains(Condition.Poison))).ToList();
            return targets.IndexOf(unaffectedEnemies.Random());
        }

        protected override bool InternalValidSelection(ColossoFighter user)
        {
            return user.Party.Any(s => s.HasCondition(TargetConditions)) ||
                (user.Party.Any(s => s.HasCondition(Condition.Venom)) && TargetConditions.Contains(Condition.Poison));
        }
    }
}