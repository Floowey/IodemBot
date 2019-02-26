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
        public static IEffect EffectFactory(string Identifier, params object[] args)
        {
            switch (Identifier)
            {
                case "Condition":
                    if (args.Length == 2 && args[0] is string && args[1] is int)
                        return new ConditionEffect((string)args[0], (int)args[1]);
                    else
                        break;

                case "Stat":
                    if (args.Length == 2 && args[0] is string && args[1] is double)
                        return new StatEffect((string)args[0], (double)args[1]);

                    else if (args.Length == 3 && args[0] is string && args[1] is double && args[2] is bool)
                        return new StatEffect((string)args[0], (double)args[1], (bool) args[2]);

                    else return new NoEffect();

                default: return new NoEffect();
            }
            return new NoEffect();
        }
    }

    
}
