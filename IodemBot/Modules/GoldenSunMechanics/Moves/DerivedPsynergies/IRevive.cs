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
        protected IRevive(string name, string emote, Element element, uint PPCost) : base(name, emote, Target.ownSingle, 1, element, PPCost)
        {
        }

        public override List<string> Use(ColossoFighter User)
        {
            List<string> log = new List<string>();
            var res = PPCheck(User);
            log.AddRange(res.Item2);
            if (!res.Item1) return log;

            log.Add($"{emote} {User.name} casts {this.name}.");
            var targetTeam = User.battle.getTeam(User.party);
            var target = targetTeam[targetNr];
            bool wasDead = !target.IsAlive();
            log.AddRange(target.revive(75));
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
