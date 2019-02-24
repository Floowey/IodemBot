using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColossoTest.Colosso
{
    public class HealPsynergy : Psynergy
    {
        public bool singleTarget;
        public int percentage;
        public int healPower;

        public HealPsynergy(string name, string emote, Target targetType, uint range, Element element, uint PPCost, int healPower, int percentage, bool singleTarget) : base(name, emote, targetType, range, element, PPCost)
        {
            this.percentage = percentage;
            this.healPower = healPower;
            this.singleTarget = singleTarget;
        }

        public override List<string> Use(ColossoFighter User)
        {
            List<string> log = new List<string>();
            if (User.stats.PP <= PPCost)
            {
                log.Add($"{User.name} has not enough PP to cast {this.name}.");
                return log;
            }
            User.stats.PP -= PPCost;

            uint Power = User.elstats.GetPower(element);
            log.Add($"{User.name} casts {this.name}!");

            var targetTeam = User.battle.getTeam(User.party);
            List<ColossoFighter> targets = new List<ColossoFighter>();
            if (singleTarget)
            {
                targets.Add(targetTeam[targetNr]);
            } else
            {
                targets.AddRange(targetTeam);
            }

            foreach (var p in targets)
            {
                var HPtoHeal = healPower * Power / 100 + p.stats.maxHP * percentage / 100;
                p.heal((uint) HPtoHeal);
                if (p.stats.HP == p.stats.maxHP)
                {
                    log.Add($"{p.name}'s HP was fully restored!");
                }
                else if (p.stats.HP == 0)
                {
                    log.Add($"It had no effect.");
                }
                else
                {
                    log.Add($"{p.name} recovers {HPtoHeal} HP!");
                }
            }
            return log;
        }
    }
}
