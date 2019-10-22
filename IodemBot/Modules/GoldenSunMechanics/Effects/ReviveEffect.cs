using IodemBot.Extensions;
using IodemBot.Modules.ColossoBattles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class ReviveEffect : Effect
    {
        public override string Type { get; } = "Revive";
        private uint Percentage { get; set; } = 50;

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            List<string> log = new List<string>();
            bool wasDead = !Target.IsAlive;
            log.AddRange(Target.Revive(Percentage));
            if (wasDead)
            {
                if (User is PlayerFighter)
                {
                    ((PlayerFighter)User).battleStats.Revives++;
                }
            }
            return log;
        }

        public override string ToString()
        {
            return $"Revive the target to {Percentage}% of its maximum Health";
        }

        protected override int InternalChooseBestTarget(List<ColossoFighter> targets)
        {
            if (targets.All(s => s.IsAlive))
            {
                return 0;
            }

            var deadFriends = targets.Where(s => !s.IsAlive).ToList();
            Console.WriteLine($"{deadFriends.Count} dead targets.");
            return targets.IndexOf(deadFriends.Random());
        }

        protected override bool InternalValidSelection(ColossoFighter user)
        {
            return user.GetTeam().Any(s => !s.IsAlive);
        }
    }
}