using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public abstract class IEffect
    {
        public enum TimeToActivate { beforeDamge, afterDamage };

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

                case "ReduceDamage":
                    return new ReduceDamageEffect(args);

                default: return new NoEffect();
            }
        }

        protected virtual bool InternalValidSelection(ColossoFighter user)
        {
            return true;
        }

        protected virtual int InternalChooseBestTarget(List<ColossoFighter> targets)
        {
            return Global.random.Next(0, targets.Count);
        }

        internal int ChooseBestTarget(List<ColossoFighter> targets)
        {
            return InternalChooseBestTarget(targets);
        }

        internal bool ValidSelection(ColossoFighter User)
        {
            return InternalValidSelection(User);
        }
    }

    public struct EffectImage
    {
        public string id { get; set; }
        public string[] args { get; set; }
    }
}