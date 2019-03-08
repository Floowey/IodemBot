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
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<StatusPsynergy>(serialized);
            //return MemberwiseClone();
        }

        public override void InternalChooseBestTarget(ColossoFighter User)
        {
            targetNr = effects[0].ChooseBestTarget(onEnemy ? User.getEnemies() : User.getTeam());
        }

        public override bool InternalValidSelection(ColossoFighter User)
        {
            return effects.All(s => s.ValidSelection(User));
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

        public override string ToString()
        {
            string target = "";
            switch (targetType)
            {
                case Target.self: target = "the User"; break;
                case Target.ownSingle: target = "a party member"; break;
                case Target.ownAll: target = "the Party"; break;
                case Target.otherSingle: target = "an enemy"; break;
                case Target.otherRange: target = "a range of enemies"; break;
                case Target.otherAll: target = "all enemies"; break;
            }
            return $"Apply an Effect to {target}.";
        }
    }
}
