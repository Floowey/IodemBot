using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class RestoreEffect : IEffect
    {
        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            Target.RemoveAllConditions();
            return new List<string>() { $"{Target.name}'s Conditions were cured." };
        }
    }
}
