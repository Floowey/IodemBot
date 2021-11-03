using System.Collections.Generic;
using IodemBot.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class Nothing : Move
    {
        public Nothing()
        {
            Name = "Nothing";
            Emote = "😶";
            TargetType = TargetType.PartySelf;
        }

        public override object Clone()
        {
            return new Nothing();
        }

        public override void InternalChooseBestTarget(ColossoFighter user)
        {
            user.SetTarget(0);
        }

        public override bool InternalValidSelection(ColossoFighter user)
        {
            return true;
        }

        protected override List<string> InternalUse(ColossoFighter user)
        {
            return new List<string>();
        }
    }
}