using System.Collections.Generic;
using System.Linq;
using IodemBot.Extensions;
using IodemBot.ColossoBattles;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class MysticCallEffect : Effect
    {
        public override string Type { get; } = "MysticCall";
        [JsonProperty] private List<string> EnemyNames { get; set; }
        private List<NPCEnemy> friends;

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            List<string> log = new List<string>();
            friends ??= EnemyNames.Select(e => EnemiesDatabase.GetEnemy(e)).ToList();
            if (friends.Any(e => e.Name.Equals(Target.Name)))
            {
                //log.Add($"{Target.Name} is {(Target.IsAlive ? "alive" : "dead")}");
                var Replacement = (NPCEnemy)EnemiesDatabase.GetEnemy(EnemyNames.Random()).Clone();
                Target.ReplaceWith(Replacement);
                log.Add($"{Target.Name} appears!");
            }
            return log;
        }

        protected override int InternalChooseBestTarget(List<ColossoFighter> targets)
        {
            friends ??= EnemyNames.Select(e => EnemiesDatabase.GetEnemy(e)).ToList();
            var deadFriends = targets.Where(s => friends.Any(f => f.Name.Equals(s.Name) && !s.IsAlive)).ToList();
            //Console.WriteLine($"{deadFriends.Count} dead targets.({targets.First().Name})");
            return targets.IndexOf(deadFriends.Random());
        }

        protected override bool InternalValidSelection(ColossoFighter user)
        {
            friends ??= EnemyNames.Select(e => EnemiesDatabase.GetEnemy(e)).ToList();
            return user.Party.Any(s => friends.Any(f => f.Name.Equals(s.Name) && !s.IsAlive));
        }
    }
}