using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class ChancetoOHKOEffect : IEffect
    {
        int probability = 0;
        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            if (Global.random.Next(1, 100) <= probability)
            {
                Target.Kill();
                return new List<string>() { $"{Target.name}'s life was taken." };
            }
            else return new List<string>();
        }
        public ChancetoOHKOEffect(object[] args)
        {
            timeToActivate = TimeToActivate.beforeDamge;
            if(args.Length == 1 && args[0] is int)
            {
                this.probability = (int)args[0];
            }
        }
    }
}
