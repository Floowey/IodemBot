using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class RestoreEffect : IEffect
    {
        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            if (!Target.IsAlive()) return new List<string>();
            Target.RemoveAllConditions();
            return new List<string>() { $"{Target.name}'s Conditions were cured." };
        }

        public override string ToString()
        {
            return $"Restore the target from Conditions and Poison.";
        }

        protected override int InternalChooseBestTarget(List<ColossoFighter> targets)
        {
            var unaffectedEnemies = targets.Where(s => s.hasCurableCondition()).ToList();
            return targets.IndexOf(unaffectedEnemies[Global.random.Next(0, unaffectedEnemies.Count)]);
        }

        protected override bool InternalValidSelection(ColossoFighter user)
        {
            return user.getTeam().Any(s => s.hasCurableCondition());
        }
    }
}
