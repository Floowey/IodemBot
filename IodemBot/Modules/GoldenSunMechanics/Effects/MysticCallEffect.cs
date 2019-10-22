using IodemBot.Extensions;
using IodemBot.Modules.ColossoBattles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class MysticCallEffect : Effect
    {
        public override string Type { get; } = "MysticCall";
        private List<string> EnemyNames { get; set; }

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            List<string> log = new List<string>();
            if (EnemyNames.Select(e => EnemiesDatabase.GetEnemy(e)).ToList().Any(e => e.Name.Equals(Target.Name)))
            {
                log.Add($"{Target.Name} is {(Target.IsAlive ? "alive" : "dead")}");
                var Replacement = (NPCEnemy)EnemiesDatabase.GetEnemy(EnemyNames.Random()).Clone();
                Target.ReplaceWith(Replacement);
                log.Add($"{Target.Name} appears!");
            }
            return log;
        }

        protected override int InternalChooseBestTarget(List<ColossoFighter> targets)
        {
            var deadFriends = targets.Where(s => !s.IsAlive).ToList();
            Console.WriteLine($"{deadFriends.Count} dead targets.({targets.First().Name})");
            return targets.IndexOf(deadFriends.Random());
        }

        protected override bool InternalValidSelection(ColossoFighter user)
        {
            return user.GetTeam().Any(s => !s.IsAlive);
        }
    }
}