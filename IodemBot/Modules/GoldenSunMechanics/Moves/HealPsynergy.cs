using IodemBot.Modules.ColossoBattles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Modules.GoldenSunMechanics
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

        public override object Clone()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<HealPsynergy>(serialized);
        }

        public override List<string> Use(ColossoFighter User)
        {
            List<string> log = new List<string>();
            var res = PPCheck(User);
            log.AddRange(res.Item2);
            if (!res.Item1) return log;

            log.Add($"{emote} {User.name} casts {this.name}!");

            uint Power = User.elstats.GetPower(element);
            List<ColossoFighter> targets = getTarget(User);

            foreach (var p in targets)
            {
                var HPtoHeal = healPower * Power / 100 + p.stats.maxHP * percentage / 100;
                log.AddRange(p.heal((uint) HPtoHeal));
                if (User is PlayerFighter) ((PlayerFighter)User).avatar.healedHP(HPtoHeal);
            }
            return log;
        }
    }
}
