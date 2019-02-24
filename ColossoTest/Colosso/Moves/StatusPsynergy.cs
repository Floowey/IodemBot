using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColossoTest.Colosso
{
    class StatusPsynergy : Psynergy
    {
        private string statToBuff;
        private double multiplier;
        private uint turns;

        public StatusPsynergy(string statToBuff, double multiplier, uint turns, string name, string emote, Target targetType, uint range, Element element, uint PPCost) : base(name, emote, targetType, range, element, PPCost)
        {
            this.statToBuff = statToBuff;
            this.multiplier = multiplier;
            this.turns = turns;
        }

        public override List<string> Use(ColossoFighter User)
        {
            List<string> log = new List<string>();
            if (User.stats.PP <= PPCost)
            {
                log.Add($"{User.name} has not enough PP to cast {this.name}.");
                return log;
            }

            //Get enemies and targeted enemies
            List<ColossoFighter> targets = new List<ColossoFighter>();
            var targetTeam = User.battle.getTeam(User.party);

            if (targetType == Target.otherAll || targetType == Target.otherRange || targetType == Target.otherSingle)
            {
                 targetTeam = User.battle.getTeam(User.enemies);
            }

            double[] actualSpread = new double[2 * range - 1];
            for (int i = -(int)range + 1; i <= range - 1; i++)
            {
                if (targetNr + i >= 0 && targetNr + i < targetTeam.Count())
                {
                    targets.Add(targetTeam[targetNr + i]);
                }
            }

            log.Add($"{User.name} casts {this.name}.");
            foreach (var t in targets)
            {
                if (!t.IsAlive()) continue;
                t.applyBuff(new Buff(statToBuff, multiplier, turns));
                log.Add($"{t.name}'s {statToBuff} {(multiplier > 1 ? "rises" : "lowers")}.");
            }

            return log;
        }
    }
}
