using System.Collections.Generic;
using System.Linq;
using IodemBot.ColossoBattles;
using IodemBot.Extensions;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class RestoreEffect : Effect
    {
        public override string Type => "Restore";

        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            if (!target.IsAlive)
            {
                return new List<string>();
            }

            target.RemoveAllConditions();
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
            return user.Party.Any(s => s.HasCurableCondition);
        }
    }
}