using System;
using System.Collections.Generic;
using System.Linq;
using IodemBot.ColossoBattles;
using IodemBot.Extensions;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class LingeringEffect : Effect
    {
        [JsonIgnore] private ColossoFighter _user;
        public override string Type => "Lingering";

        [JsonProperty] public Effect Effect { get; set; } = new NoEffect();
        [JsonProperty] public int CoolDown { get; set; }
        [JsonProperty] public int Duration { get; set; } = 1;
        [JsonProperty] public bool RemovedOnDeath { get; set; } = true;
        [JsonProperty] public string Text { get; set; } = "";
        [JsonProperty] public string RequireAlly { get; set; }
        [JsonProperty] public string AllowMultipleID { get; set; }

        private int appliedCoolDown;
        private int appliedDuration;

        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            _user = user;
            appliedCoolDown = CoolDown;
            appliedDuration = Duration;
            var log = new List<string>();
            if (user.IsAlive && !AllowMultipleID.IsNullOrEmpty() || !target.LingeringEffects.Any(l => l.AllowMultipleID == AllowMultipleID))
            {
                log.Add($"A {Effect.Type} Effect is lingering around {target.Name}");
                target.LingeringEffects.Add(this);
            }
            return log;
        }

        protected override bool InternalValidSelection(ColossoFighter user)
        {
            if (AllowMultipleID.IsNullOrEmpty())
                return true;

            var targets = user.SelectedMove.OnEnemy ? user.Enemies : user.SelectedMove.TargetType == TargetType.PartySelf ? new List<ColossoFighter>() { user } : user.Party;

            return !targets.All(e => e.LingeringEffects.Any(l => l.AllowMultipleID == AllowMultipleID));
        }

        protected override int InternalChooseBestTarget(List<ColossoFighter> targets)
        {
            if (AllowMultipleID.IsNullOrEmpty()) return base.InternalChooseBestTarget(targets);

            return targets.IndexOf(targets.Where(e => !e.LingeringEffects.Any(t => t.AllowMultipleID == AllowMultipleID)).Random());
        }

        public List<string> ApplyLingering(ColossoFighter target)
        {
            var log = new List<string>();

            if (!RequireAlly.IsNullOrEmpty() && !_user.Party.Where(p => p.IsAlive).Any(n => n.Name == RequireAlly))
                return log;

            if (appliedCoolDown > 0)
            {
                Console.WriteLine("On cooldown");
                appliedCoolDown--;
                return log;
            }

            if (appliedDuration >= 1)
            {
                appliedDuration--;
                if (!Text.IsNullOrEmpty()) log.Add(string.Format(Text, target.Name));
                log.AddRange(Effect.Apply(_user, target));
                Console.WriteLine("Applied Effect");
            }

            return log;
        }

        public override string ToString()
        {
            return $"Casts a {Effect.Type} Lingering effect over a target for {Duration} turns";
        }
    }
}