using System;
using System.Collections.Generic;
using System.Linq;
using IodemBot.Extensions;
using IodemBot.ColossoBattles;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class ReduceDamageEffect : Effect
    {
        public override string Type { get; } = "ReduceDamage";
        [JsonProperty] private int DamageReduction { get; set; } = 0;

        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            List<string> log = new List<string>();
            if (!Target.IsAlive)
            {
                return log;
            }

            Target.defensiveMult = Math.Min(Target.defensiveMult, (double)(100 - DamageReduction) / 100);

            return log;
        }

        protected override int InternalChooseBestTarget(List<ColossoFighter> targets)
        {
            var target = targets.Where(d => d.Name.Contains("Star")).FirstOrDefault() ?? targets.Where(t => t.IsAlive).Random();
            return targets.IndexOf(target);
        }

        protected override bool InternalValidSelection(ColossoFighter user)
        {
            if (base.InternalValidSelection(user))
            {
                return user.Party.Count(t => t.IsAlive) > 1;
            }
            return false;
        }

        public override string ToString()
        {
            return $"Reduces damage taken by {DamageReduction}%";
        }
    }
}