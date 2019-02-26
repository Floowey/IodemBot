using IodemBot.Modules.ColossoBattles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class Revive : IRevive
    {
        public Revive() : base("Revive", "<:Revive:536957965513785347>", new List<EffectImage>(), Element.Venus, 15)
        {
        }
    }
}
