using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Modules.ColossoBattles;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    abstract class IRevive : Psynergy
    {
        protected IRevive(string name, string emote, List<EffectImage> effectImages, Element element, uint PPCost) : base(name, emote, Target.ownSingle, 1, effectImages, element, PPCost)
        {
        }

        protected override List<string> InternalUse(ColossoFighter User)
        {
            List<string> log = new List<string>();
            var targetTeam = User.battle.getTeam(User.party);
            var target = targetTeam[targetNr];
            bool wasDead = !target.IsAlive();
            log.AddRange(target.Revive(75));
            if (wasDead)
            {
                if (User is PlayerFighter) ((PlayerFighter)User).avatar.revived();
            }
            return log;
        }

        public override object Clone()
        {
            return MemberwiseClone();
        }
    }
}
