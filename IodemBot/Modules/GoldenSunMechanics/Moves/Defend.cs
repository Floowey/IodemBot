using IodemBot.Modules.ColossoBattles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class Defend : Move
    {
        public Defend() : base("Defend", "<:Defend:536919830507552768>", Target.self, 1)
        {
        }

        public override object Clone()
        {
            return MemberwiseClone();
        }

        protected override List<string> InternalUse(ColossoFighter User)
        {
            User.Buffs.Add(new Buff("Defense", 4, 1));
            return new List<string>();
        }
    }
}
