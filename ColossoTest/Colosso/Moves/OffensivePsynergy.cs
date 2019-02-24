using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColossoTest.Colosso
{
    public class OffensivePsynergy : Psynergy
    {
        public uint power;
        public uint addDamage;
        private bool attackBased;
        private double[] spread = new double[] { 1.0, 0.66, 0.5, 0.33, 0.25, 0.15, 0.1 };
        public OffensivePsynergy(string name, string emote, Target targetType, uint range, Element element, uint PPCost, uint power, uint addDamage) : base(name, emote, targetType, range, element, PPCost)
        {
            this.power = power;
            this.addDamage = addDamage;
            attackBased = power == 0;
        }

        public override List<string> Use(ColossoFighter User)
        {
            //Psynergy Handling
            List<string> log = new List<string>();
            if (User.stats.PP <= PPCost)
            {
                log.Add($"{User.name} has not enough PP to cast {this.name}.");
                return log;
            }
            User.stats.PP -= PPCost;

            //Get enemies and targeted enemies
            var targetTeam = User.battle.getTeam(User.enemies);
            List<ColossoFighter> targets = new List<ColossoFighter>();


            double[] actualSpread = new double[2*range-1];
            for (int i = -(int) range + 1; i <= range -1; i++)
            {
                if(targetNr + i >= 0 && targetNr + i < targetTeam.Count())
                {
                    targets.Add(targetTeam[targetNr + i]);
                    actualSpread[targets.Count()-1] = spread[Math.Abs(i)];
                }
            }

            int ii = 0;
            log.Add($"{User.name} casts {this.name}.");
            foreach (var t in targets)
            {
                if (!t.IsAlive()) continue;
                var damage = (new Random()).Next(0, 4);
                damage += attackBased ? Math.Max(0, ((int)User.stats.Atk - (int)t.stats.Def) / 2) : 0;
                damage += (int)addDamage;

                var elFactor = Math.Max(0.0, (int)User.elstats.GetPower(element) - (int)t.elstats.GetRes(element));

                damage += (int)(actualSpread[ii] * power * (1 + elFactor / (attackBased ? 400 : 200)));
                t.dealDamage((uint)damage);
                var punctuation = "!";

                if (t.elstats.GetRes(element) == t.elstats.leastRes()) punctuation = "!!!";
                if (t.elstats.GetRes(element) == t.elstats.highestRes()) punctuation = ".";
                log.Add($"{t.name} takes {damage} damage{punctuation}");

                if(t.stats.HP == 0)
                {
                    log.Add($"{t.name} goes down.");
                }

                ii++;
            }

            return log;
        }
    }
}
