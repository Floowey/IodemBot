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

        public StatusPsynergy(string name, string emote, Target targetType, uint range, List<EffectImage> effectImages, Element element, uint PPCost) : base(name, emote, targetType, range, effectImages, element, PPCost)
        {
        }

        public override object Clone()
        {
            //var serialized = JsonConvert.SerializeObject(this);
            //return JsonConvert.DeserializeObject<StatusPsynergy>(serialized);
            return MemberwiseClone();
        }

        protected override List<string> InternalUse(ColossoFighter User)
        {
            List<string> log = new List<string>();
            //Get enemies and targeted enemies
            List<ColossoFighter> targets = getTarget(User);

            foreach (var t in targets)
            {
                effects.ForEach(e => log.AddRange(e.Apply(User, t)));
            }

            return log;
        }
    }
}
