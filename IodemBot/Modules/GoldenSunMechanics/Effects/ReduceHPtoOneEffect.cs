using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class ReduceHPtoOneEffect : IEffect
    {
        private int probability = 10;

        public ReduceHPtoOneEffect(object[] args)
        {
            if (args.Length == 1 && args[0] is int)
            {
                probability = (int)args[0];
            }
        }

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            var log = new List<string>();
            if (Target.isImmuneToEffects) return log;
            if (Target.IsAlive())
            {
                if (Global.random.Next(1, 100) <= probability) Target.stats.HP = 1;
            }
            return log;
        }
    }
}
