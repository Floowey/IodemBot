using System;
using System.Collections.Generic;
using System.Linq;
using IodemBot.ColossoBattles;
using IodemBot.Extensions;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class ReduceDamageEffect : Effect
    {
        public override string Type => "ReduceDamage";
        [JsonProperty] private int DamageReduction { get; set; }

        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            var log = new List<string>();
            if (!target.IsAlive) return log;

            target.DefensiveMult = Math.Min(target.DefensiveMult, (double)(100 - DamageReduction) / 100);

            return log;
        }

        protected override int InternalChooseBestTarget(List<ColossoFighter> targets)
        {
            return ChooseAliveVIPTarget(targets);
        }

        protected override bool InternalValidSelection(ColossoFighter user)
        {
            if (base.InternalValidSelection(user))
                return user is PlayerFighter || user.Party.Count(t => t.IsAlive) > 1;
            return false;
        }

        public override string ToString()
        {
            return $"Reduces damage taken by {DamageReduction}%";
        }
    }
}