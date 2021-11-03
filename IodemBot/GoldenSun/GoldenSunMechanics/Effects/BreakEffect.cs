using System.Collections.Generic;
using IodemBot.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class BreakEffect : Effect
    {
        public override string Type => "Break";

        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            List<string> log = new List<string>();
            List<Buff> newBuffs = new List<Buff>();
            if (!target.IsAlive)
            {
                return log;
            }

            foreach (var b in target.Buffs)
            {
                if (b.Multiplier > 1)
                {
                    log.Add($"{target.Name}'s Boost to {b.Stat} normalizes");
                }
                else
                {
                    newBuffs.Add(b);
                }
            }
            if (user is PlayerFighter p)
            {
                p.BattleStats.Supported++;
            }
            target.Buffs = newBuffs;
            return log;
        }

        public override string ToString()
        {
            return "Remove Stat buffs of enemies";
        }
    }
}