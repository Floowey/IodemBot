﻿using System.Collections.Generic;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class BreakEffect : Effect
    {
        public override string Type { get; } = "Break";

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            List<string> log = new List<string>();
            List<Buff> newBuffs = new List<Buff>();
            if (!Target.IsAlive)
            {
                return log;
            }

            foreach (var b in Target.Buffs)
            {
                if (b.multiplier > 1)
                {
                    log.Add($"{Target.Name}'s Boost to {b.stat} normalizes");
                }
                else
                {
                    newBuffs.Add(b);
                }
            }
            if (User is PlayerFighter p)
            {
                p.battleStats.Supported++;
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