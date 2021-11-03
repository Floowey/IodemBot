using System.Collections.Generic;
using System.Linq;
using IodemBot.ColossoBattles;
using IodemBot.Extensions;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class MysticCallEffect : Effect
    {
        public override string Type => "MysticCall";
        [JsonProperty] private List<string> EnemyNames { get; set; }
        private List<NpcEnemy> _friends;

        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            List<string> log = new();
            _friends ??= EnemyNames.Select(EnemiesDatabase.GetEnemy).ToList();
            if (!_friends.Any(e => e.Name.Equals(target.Name))) return log;
            //log.Add($"{Target.Name} is {(Target.IsAlive ? "alive" : "dead")}");
            var replacement = (NpcEnemy)EnemiesDatabase.GetEnemy(EnemyNames.Random()).Clone();
            target.ReplaceWith(replacement);
            log.Add($"{target.Name} appears!");
            return log;
        }

        protected override int InternalChooseBestTarget(List<ColossoFighter> targets)
        {
            _friends ??= EnemyNames.Select(EnemiesDatabase.GetEnemy).ToList();
            var deadFriends = targets.Where(s => _friends.Any(f => f.Name.Equals(s.Name) && !s.IsAlive)).ToList();
            //Console.WriteLine($"{deadFriends.Count} dead targets.({targets.First().Name})");
            return targets.IndexOf(deadFriends.Random());
        }

        protected override bool InternalValidSelection(ColossoFighter user)
        {
            _friends ??= EnemyNames.Select(EnemiesDatabase.GetEnemy).ToList();
            return user.Party.Any(s => _friends.Any(f => f.Name.Equals(s.Name) && !s.IsAlive));
        }
    }
}