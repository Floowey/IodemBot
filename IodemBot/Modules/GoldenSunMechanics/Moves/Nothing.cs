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
        public Nothing() : base("Nothing", "😶", Target.self, 0, new List<EffectImage>())
        {
        }

        public override object Clone()
        {
            return new Nothing();
        }

        public override void InternalChooseBestTarget(ColossoFighter User)
        {
            return;
        }

        public override bool InternalValidSelection(ColossoFighter User)
        {
            return true;
        }

        protected override List<string> InternalUse(ColossoFighter User)
        {
            return new List<string>() ;
        }


    }
}
