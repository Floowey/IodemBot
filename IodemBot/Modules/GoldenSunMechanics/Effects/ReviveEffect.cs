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

        public ReviveEffect(object[] args)
        {
            if(args.Length == 1 && args[0] is uint)
            {
                this.percentage = (uint)args[0];
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
    }
}
