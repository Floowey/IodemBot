using System.Collections.Generic;
using System.Linq;
using IodemBot.ColossoBattles;
using IodemBot.Extensions;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class ReviveEffect : Effect
    {
        public override string Type => "Revive";
        [JsonProperty] private uint Percentage { get; set; } = 50;
        [JsonProperty] private uint Probability { get; set; } = 100;

        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            List<string> log = new List<string>();
            bool wasDead = !target.IsAlive;
            if (Global.RandomNumber(0, 100) > Probability)
            {
                log.Add($"But it has no effect on {target.Name}");
                return log;
            }
            log.AddRange(target.Revive(Percentage));
            if (wasDead)
            {
                if (user is PlayerFighter p)
                {
                    p.BattleStats.Revives++;
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
            return user.Party.Any(s => !s.IsAlive);
        }
    }
}