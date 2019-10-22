using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class Nothing : Move
    {
        public Nothing()
        {
            Name = "Nothing";
            Emote = "😶";
            TargetType = Target.self;
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
            return new List<string>();
        }
    }
}