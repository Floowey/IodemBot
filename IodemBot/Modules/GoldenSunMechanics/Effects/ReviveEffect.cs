using IodemBot.Modules.ColossoBattles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class ReviveEffect : IEffect
    {
        private readonly uint percentage;

        public ReviveEffect(string[] args)
        {
            if (args.Length == 1)
            {
                uint.TryParse(args[0], out percentage);
            }
        }

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            List<string> log = new List<string>();
            bool wasDead = !Target.IsAlive();
            log.AddRange(Target.Revive(percentage));
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
            return $"Revive the target to {percentage}% of its maximum Health";
        }

        protected override int InternalChooseBestTarget(List<ColossoFighter> targets)
        {
            var deadFriends = targets.Where(s => !s.IsAlive()).ToList();
            Console.WriteLine($"{deadFriends.Count} dead targets.");
            return targets.IndexOf(deadFriends[Global.Random.Next(0, deadFriends.Count)]);
        }

        protected override bool InternalValidSelection(ColossoFighter user)
        {
            return user.GetTeam().Any(s => !s.IsAlive());
        }
    }
}