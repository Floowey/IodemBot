using System.Collections.Generic;
using System.Linq;
using IodemBot.ColossoBattles;
using IodemBot.Extensions;
using JsonSubTypes;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    [JsonConverter(typeof(JsonSubtypes), "Type")]
    [JsonSubtypes.FallBackSubType(typeof(NoEffect))]
    [JsonSubtypes.KnownSubType(typeof(AddDamageEffect), "AddDamage")]
    [JsonSubtypes.KnownSubType(typeof(AttackWithTeammateEffect), "AttackWithTeammate")]
    [JsonSubtypes.KnownSubType(typeof(BreakEffect), "Break")]
    [JsonSubtypes.KnownSubType(typeof(ChancetoOhkoEffect), "OHKO")]
    [JsonSubtypes.KnownSubType(typeof(CrucibleEffect), "Crucible")]
    [JsonSubtypes.KnownSubType(typeof(ConditionEffect), "Condition")]
    [JsonSubtypes.KnownSubType(typeof(CounterEffect), "Counter")]
    [JsonSubtypes.KnownSubType(typeof(DarkEffect), "Dark")]
    [JsonSubtypes.KnownSubType(typeof(DjinnManipEffect), "DjinnManip")]
    [JsonSubtypes.KnownSubType(typeof(HpDrainEffect), "HPDrain")]
    [JsonSubtypes.KnownSubType(typeof(MayIgnoreDefenseEffect), "IgnoreDefense")]
    [JsonSubtypes.KnownSubType(typeof(MultiplyDamageEffect), "MultiplyDamage")]
    [JsonSubtypes.KnownSubType(typeof(MysticCallEffect), "MysticCall")]
    [JsonSubtypes.KnownSubType(typeof(NoEffect), "Nothing")]
    [JsonSubtypes.KnownSubType(typeof(PpDrainEffect), "PPDrain")]
    [JsonSubtypes.KnownSubType(typeof(ReduceDamageEffect), "ReduceDamage")]
    [JsonSubtypes.KnownSubType(typeof(ReduceHPtoOneEffect), "ReduceHPToOne")]
    [JsonSubtypes.KnownSubType(typeof(RestoreEffect), "Restore")]
    [JsonSubtypes.KnownSubType(typeof(ReviveEffect), "Revive")]
    [JsonSubtypes.KnownSubType(typeof(StatEffect), "Stat")]
    [JsonSubtypes.KnownSubType(typeof(UserDiesEffect), "UserDies")]
    [JsonSubtypes.KnownSubType(typeof(DealDamageEffect), "DealDamage")]
    [JsonSubtypes.KnownSubType(typeof(HealEffect), "Heal")]
    [JsonSubtypes.KnownSubType(typeof(LingeringEffect), "Lingering")]
    [JsonSubtypes.KnownSubType(typeof(LibraEffect), "Libra")]
    public abstract class Effect
    {
        public TimeToActivate ActivationTime { get; set; } = TimeToActivate.AfterDamage;
        public virtual string Type => "Nothing";

        public bool OnTarget { get; set; } = true;

        public abstract List<string> Apply(ColossoFighter user, ColossoFighter target);

        protected virtual bool InternalValidSelection(ColossoFighter user)
        {
            return true;
        }

        protected virtual int InternalChooseBestTarget(List<ColossoFighter> targets)
        {
            if (!targets.Any(d => d.IsAlive))
            {
                return 0;
            }

            return targets.IndexOf(targets.Where(t => t.IsAlive).Random());
        }

        protected virtual int ChooseAliveVIPTarget(List<ColossoFighter> targets)
        {
            var aliveFriends = targets.Where(f => f.IsAlive).ToList();
            if (!aliveFriends.Any())
            {
                return 0;
            }

            aliveFriends = aliveFriends.OrderBy(s => s.Stats.HP / s.Stats.MaxHP).ThenBy(s => s.Stats.HP).ToList();

            return targets.IndexOf(aliveFriends.Any(d => d.Tags.Contains("VIP"))
                ? targets.Where(d => d.Tags.Contains("VIP")).Random()
                : aliveFriends.First());
        }

        internal int ChooseBestTarget(List<ColossoFighter> targets)
        {
            return InternalChooseBestTarget(targets);
        }

        internal bool ValidSelection(ColossoFighter user)
        {
            return InternalValidSelection(user);
        }

        public override string ToString()
        {
            return "Unspecified Effect";
        }
    }
}