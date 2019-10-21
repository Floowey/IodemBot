using IodemBot.Extensions;
using IodemBot.Modules.ColossoBattles;
using JsonSubTypes;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Modules.GoldenSunMechanics
{
    [JsonConverter(typeof(JsonSubtypes), "Type")]
    public abstract class IEffect
    {
        public enum TimeToActivate { beforeDamge, afterDamage };

        public TimeToActivate timeToActivate = TimeToActivate.afterDamage;

        public abstract List<string> Apply(ColossoFighter User, ColossoFighter Target);

        public virtual string Type { get; } = "Nothing";

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