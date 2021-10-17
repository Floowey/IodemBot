using System.Collections.Generic;
using System.Linq;
using IodemBot.Extensions;
using IodemBot.ColossoBattles;
using JsonSubTypes;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    [JsonConverter(typeof(JsonSubtypes), "Type")]
    [JsonSubtypes.FallBackSubType(typeof(NoEffect))]
    [JsonSubtypes.KnownSubType(typeof(AddDamageEffect), "AddDamage")]
    [JsonSubtypes.KnownSubType(typeof(AttackWithTeammateEffect), "AttackWithTeammate")]
    [JsonSubtypes.KnownSubType(typeof(BreakEffect), "Break")]
    [JsonSubtypes.KnownSubType(typeof(ChancetoOHKOEffect), "OHKO")]
    [JsonSubtypes.KnownSubType(typeof(ConditionEffect), "Condition")]
    [JsonSubtypes.KnownSubType(typeof(CounterEffect), "Counter")]
    [JsonSubtypes.KnownSubType(typeof(HPDrainEffect), "HPDrain")]
    [JsonSubtypes.KnownSubType(typeof(MayIgnoreDefenseEffect), "IgnoreDefense")]
    [JsonSubtypes.KnownSubType(typeof(MultiplyDamageEffect), "MultiplyDamage")]
    [JsonSubtypes.KnownSubType(typeof(MysticCallEffect), "MysticCall")]
    [JsonSubtypes.KnownSubType(typeof(NoEffect), "Nothing")]
    [JsonSubtypes.KnownSubType(typeof(PPDrainEffect), "PPDrain")]
    [JsonSubtypes.KnownSubType(typeof(ReduceDamageEffect), "ReduceDamage")]
    [JsonSubtypes.KnownSubType(typeof(ReduceHPtoOneEffect), "ReduceHPToOne")]
    [JsonSubtypes.KnownSubType(typeof(RestoreEffect), "Restore")]
    [JsonSubtypes.KnownSubType(typeof(ReviveEffect), "Revive")]
    [JsonSubtypes.KnownSubType(typeof(StatEffect), "Stat")]
    [JsonSubtypes.KnownSubType(typeof(UserDiesEffect), "UserDies")]
    [JsonSubtypes.KnownSubType(typeof(DealDamageEffect), "DealDamage")]
    [JsonSubtypes.KnownSubType(typeof(HealEffect), "Heal")]
    [JsonSubtypes.KnownSubType(typeof(LingeringEffect), "Lingering")]
    public abstract class Effect
    {
        public TimeToActivate ActivationTime { get; set; } = TimeToActivate.afterDamage;
        public virtual string Type { get; } = "Nothing";

        public bool OnTarget { get; set; } = true;
        public abstract List<string> Apply(ColossoFighter User, ColossoFighter Target);

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
}