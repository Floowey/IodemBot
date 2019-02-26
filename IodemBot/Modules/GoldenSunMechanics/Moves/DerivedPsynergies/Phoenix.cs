using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class Phoenix : IRevive
    {
        public Phoenix() : base("Phoenix", "<:Phoenix:539166682132906005>", new List<EffectImage>(), Element.Mars, 10)
        {
        }
    }
}
