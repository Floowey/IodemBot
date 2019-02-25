using IodemBot.Modules.ColossoBattles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class Nothing : Move
    {
        public Nothing() : base("Nothing", "😶", Target.self, 0)
        {
        }

        public override object Clone()
        {
            return MemberwiseClone();
        }

        protected override List<string> InternalUse(ColossoFighter User)
        {
            return new List<string>() ;
        }


    }
}
