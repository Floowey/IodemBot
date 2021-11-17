using System;
using System.Collections.Generic;
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
        [JsonProperty] public bool AllowMultiple { get; set; }
        [JsonIgnore] public Guid InstanceID { get; private set; }
        private int appliedCoolDown;
        private int appliedDuration;

        public LingeringEffect()
        {
            InstanceID = Guid.NewGuid();
        }

        public override List<string> Apply(ColossoFighter user, ColossoFighter target)
        {
            _user = user;
            appliedCoolDown = CoolDown;
            appliedDuration = Duration;
            target.LingeringEffects.Add(this);
            return new List<string> { $"A {Effect.Type} Effect is lingering around {target.Name}" };
        }

        public List<string> ApplyLingering(ColossoFighter target)
        {
            var log = new List<string>();
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