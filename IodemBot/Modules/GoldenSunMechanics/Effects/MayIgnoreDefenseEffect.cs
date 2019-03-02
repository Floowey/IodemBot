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

        public MayIgnoreDefenseEffect(string[] args)
        {
            timeToActivate = TimeToActivate.beforeDamge;
            if(args.Length == 2){
                int.TryParse(args[0], out ignorePercent);
                int.TryParse(args[1], out probability);
            }
        }

        public override string ToString()
        {
            return $"{(probability != 100 ? "Chance to ignore ": "Ignore ")} {ignorePercent} of Defense";
        }
    }
}
