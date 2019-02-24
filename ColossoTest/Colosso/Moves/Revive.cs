using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColossoTest.Colosso
{
    class Revive : Psynergy
    {
        public Revive() : base("Revive", "🔱", Target.ownSingle, 1, Element.Venus, 15)
        {
        }

        public override List<string> Use(ColossoFighter User)
        {
            List<string> log = new List<string>();
            if (User.stats.PP <= PPCost)
            {
                log.Add($"{User.name} has not enough PP to cast {this.name}!");
                return log;
            }
            User.stats.PP -= PPCost;

            log.Add($"{User.name} casts {this.name}.");
            var targetTeam = User.battle.getTeam(User.party);
            var target = targetTeam[targetNr];
            target.revive(75);
            return log;
        }
    }
}
