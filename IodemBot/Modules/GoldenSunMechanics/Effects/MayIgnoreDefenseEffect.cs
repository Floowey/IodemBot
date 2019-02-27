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
        int probability = 10;
        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            if(Global.random.Next(1, 100) <= probability)
                Target.ignoreDefense = (1 - ignorePercent / 100);
            return new List<string>();
        }

        public MayIgnoreDefenseEffect(object[] args)
        {
            timeToActivate = TimeToActivate.beforeDamge;
            if(args.Length == 2 && args[0] is int && args[1] is int)
            {
                ignorePercent = (int)args[0];
                probability = (int)args[1];
            }
        }
    }
}
