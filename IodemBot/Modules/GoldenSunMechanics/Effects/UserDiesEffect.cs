using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class UserDiesEffect : IEffect
    {
        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            User.Kill();
            return new List<string>() { $"{User.name}'s goes down from exhaustion." };
        }
    }
}
