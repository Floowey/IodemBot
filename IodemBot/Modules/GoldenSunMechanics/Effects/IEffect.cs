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
        public TimeToActivate timeToActivate = TimeToActivate.afterDamage;

        public abstract List<string> Apply(ColossoFighter User, ColossoFighter Target);
        public static IEffect EffectFactory(string Identifier, params object[] args)
        {
            switch (Identifier)
            {
                case "Condition":
                    return new ConditionEffect(args);

                case "Stat":
                    return new StatEffect(args);

                case "ChanceToOHKO":
                    return new ChancetoOHKOEffect(args);

                default: return new NoEffect();
            }
        }
    }

    
}
