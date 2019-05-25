using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class BreakEffect : IEffect
    {
        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            List<string> log = new List<string>();
            List<Buff> newBuffs = new List<Buff>();
            if (!Target.IsAlive())
            {
                return log;
            }

            foreach (var b in Target.Buffs)
            {
                if (b.multiplier > 1)
                {
                    log.Add($"{Target.name}'s Boost to {b.stat} normalizes");
                }
                else
                {
                    newBuffs.Add(b);
                }
            }
            if (User is PlayerFighter)
            {
                ((PlayerFighter)User).battleStats.Supported++;
            }
            Target.Buffs = newBuffs;
            return log;
        }

        public override string ToString()
        {
            return "Remove Stat buffs of enemies";
        }
    }
}