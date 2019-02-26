using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class MayIgnoreDefenseEffect : IEffect
    {
        int ignorePercent = 20;
        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            Target.ignoreDefense = (1 - ignorePercent / 100);
            return new List<string>();
        }

        public MayIgnoreDefenseEffect(object[] args)
        {
            if(args.Length == 1 && args[0] is int)
            {
                ignorePercent = (int)args[0];
            }
        }
    }
}
