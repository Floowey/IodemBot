using IodemBot.Modules.ColossoBattles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public abstract class IEffect
    {
        public enum TimeToActivate { beforeDamge, afterDamage};
        public TimeToActivate timeToActivate;

        public abstract List<string> Apply(ColossoFighter User, ColossoFighter Target);
        public static IEffect EffectFactory(string Identifier, string subIdentifier, int param)
        {
            return new NoEffect();
        }
    }

    
}
