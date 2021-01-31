using System;
using System.Collections.Generic;
using System.Linq;
using IodemBot.Extensions;
using IodemBot.Modules.ColossoBattles;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class ReviveEffect : Effect
    {
        public override string Type { get; } = "Revive";
        [JsonProperty] private uint Percentage { get; set; } = 50;
        [JsonProperty] private uint Probability { get; set; } = 100;

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            List<string> log = new List<string>();
            bool wasDead = !Target.IsAlive;
            if (Global.RandomNumber(0, 100) > Probability)
            {
                log.Add($"But it has no effect on {Target.Name}");
                return log;
            }
            log.AddRange(Target.Revive(Percentage));
            if (wasDead)
            {
                if (User is PlayerFighter p)
                {
                    p.battleStats.Revives++;
                }
            }
            return log;
        }

        public override string ToString()
        {
            return $"{Probability}% chance to revive the target to {Percentage}% of its maximum Health";
        }

        protected override int InternalChooseBestTarget(List<ColossoFighter> targets)
        {
            if (targets.All(s => s.IsAlive))
            {
                return 0;
            }

            var deadFriends = targets.Where(s => !s.IsAlive).ToList();
            //Console.WriteLine($"{deadFriends.Count} dead targets.");
            return targets.IndexOf(deadFriends.Random());
        }

        protected override bool InternalValidSelection(ColossoFighter user)
        {
            return user.GetTeam().Any(s => !s.IsAlive);
        }
    }
}