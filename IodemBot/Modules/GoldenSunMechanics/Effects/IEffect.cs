using IodemBot.Extensions;
using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;
using System.Linq;

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
                case "AttackWithTeammate":
                    return new AttackWithTeammateEffect();

                case "Break":
                    return new BreakEffect();

                case "ChanceToOHKO":
                    return new ChancetoOHKOEffect(args);

                case "Condition":
                    return new ConditionEffect(args);

                case "Counter":
                    return new CounterEffect();

                case "HPDrain":
                    return new HPDrainEffect(args);

                case "MayIgnoreDefense":
                    return new MayIgnoreDefenseEffect(args);

                case "MultiplyDamage":
                    return new MultiplyDamageEffect(args);

                case "PPDrain":
                    return new PPDrainEffect(args);

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

                case "AddDamage":
                    return new AddDamageEffect(args);

                case "MysticCall":
                    return new MysticCallEffect(args);

                case "NoEffect":
                default: return new NoEffect();
            }
        }

        protected virtual bool InternalValidSelection(ColossoFighter user)
        {
            return true;
        }

        protected virtual int InternalChooseBestTarget(List<ColossoFighter> targets)
        {
            if (targets.Where(d => d.IsAlive).Count() == 0)
            {
                return 0;
            }

            return targets.IndexOf(targets.Where(t => t.IsAlive).Random());
        }

        internal int ChooseBestTarget(List<ColossoFighter> targets)
        {
            return InternalChooseBestTarget(targets);
        }

        internal bool ValidSelection(ColossoFighter User)
        {
            return InternalValidSelection(User);
        }

        public override string ToString()
        {
            return "Unspecified Effect";
        }
    }

    public struct EffectImage
    {
        public string Id { get; set; }
        public string[] Args { get; set; }
    }
}