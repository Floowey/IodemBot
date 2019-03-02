using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class ReviveEffect : IEffect
    {
        private uint percentage;

        public ReviveEffect(string[] args)
        {
            if(args.Length == 1)
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
                if (User is PlayerFighter) ((PlayerFighter)User).avatar.revived();
            }
            return log;
        }

        public override string ToString()
        {
            return $"Revive the target to {percentage}% of it's maximum Health.";
        }
    }
}
