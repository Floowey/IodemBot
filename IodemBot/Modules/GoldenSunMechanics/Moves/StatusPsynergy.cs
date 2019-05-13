using IodemBot.Modules.ColossoBattles;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class StatusPsynergy : Psynergy
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
            if (effects.Count > 0)
            {
                targetNr = effects[0].ChooseBestTarget(OnEnemy ? User.GetEnemies() : User.GetTeam());
            }
            else
            {
                targetNr = 0;
            }
        }

        public override bool InternalValidSelection(ColossoFighter User)
        {
            if (User.stats.PP < PPCost)
            {
                return false;
            }

            if (effects.Count > 0)
            {
                return effects[0].ValidSelection(User);
            }

            return true;
        }

        protected override List<string> InternalUse(ColossoFighter User)
        {
            List<string> log = new List<string>();
            //Get enemies and targeted enemies
            List<ColossoFighter> targets = GetTarget(User);

            foreach (var t in targets)
            {
                effects.ForEach(e => log.AddRange(e.Apply(User, t)));
                if (User is PlayerFighter)
                {
                    ((PlayerFighter)User).battleStats.Supported++;
                }
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
                case Target.otherRange: target = $"a range of {range} enemies"; break;
                case Target.otherAll: target = "all enemies"; break;
            }
            return $"Apply an Effect to {target}.";
        }
    }
}