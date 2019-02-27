using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics.Effects
{
    class ReduceHPtoOneEffect : IEffect
    {
        int probability;
        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            if (Target.IsAlive())
            {
                if (Global.random.Next(1, 100) <= probability) Target.stats.HP = 1;
            }
            return new List<string>();
        }
    }
}
