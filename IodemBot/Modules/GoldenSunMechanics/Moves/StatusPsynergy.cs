using IodemBot.Modules.ColossoBattles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Modules.GoldenSunMechanics
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

        public override object Clone()
        {
            //var serialized = JsonConvert.SerializeObject(this);
            //return JsonConvert.DeserializeObject<StatusPsynergy>(serialized);
            return MemberwiseClone();
        }

        public override List<string> Use(ColossoFighter User)
        {
            List<string> log = new List<string>();
            var res = PPCheck(User);
            log.AddRange(res.Item2);
            if (!res.Item1) return log;

            //Get enemies and targeted enemies
            List<ColossoFighter> targets = getTarget(User);

            log.Add($"{emote} {User.name} casts {this.name}.");
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
