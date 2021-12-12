using System.Collections.Generic;
using IodemBot.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class NoEffect : Effect
    {
        public override string Type => "NoEffect";

        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            return new List<string>();
        }

        public override string ToString()
        {
            return "Doesn't have a effect, should probably have one. Please report.";
        }

        protected override bool InternalValidSelection(ColossoFighter user)
        {
            return false;
        }
    }
}