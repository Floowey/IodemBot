using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class BreakEffect : IEffect
    {
        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            List<string> log = new List<string>();
            List<Buff> newBuffs = new List<Buff>();
            if (!Target.IsAlive()) return log;
            foreach (var b in Target.Buffs)
            {
                if (b.multiplier > 1)
                {
                    log.Add($"{Target.name}'s Boost to {b.stat} normalizes");
                } else
                {
                    newBuffs.Add(b);
                }
            }
            Target.Buffs = newBuffs;
            return log;
        }

        public override string ToString()
        {
            return "Remove Stat buffs of enemies.";
        }
    }
}
