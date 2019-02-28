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
        public static IEffect EffectFactory(string Identifier, params string[] args)
        {
            switch (Identifier)
            {
                case "Break":
                    return new BreakEffect();
                case "ChanceToOHKO":
                    return new ChancetoOHKOEffect(args);
                case "Condition":
                    return new ConditionEffect(args);
                case "Counter":
                    return new CounterEffect();
                case "MayIgnoreDefense":
                    return new MayIgnoreDefenseEffect(args);
                case "MultiplyDamage":
                    return new MultiplyDamageEffect(args);
                case "ReduceHPtoOne":
                    return new ReduceHPtoOneEffect(args);
                case "Restore":
                    return new RestoreEffect();
                case "Revive":
                    return new ReviveEffect(args);
                case "Stat":
                    return new StatEffect(args);
                case "UserDies":
                    return new UserDiesEffect();

                default: return new NoEffect();
            }
        }
    }

    public struct EffectImage
    {
        public string id { get; set; }
        public string[] args { get; set; }
    }

    
}
