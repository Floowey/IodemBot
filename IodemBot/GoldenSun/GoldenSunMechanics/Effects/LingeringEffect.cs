using System.Collections.Generic;
using System.Linq;
using IodemBot.Extensions;
using IodemBot.ColossoBattles;
using Newtonsoft.Json;
using System;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class LingeringEffect : Effect
    {
        public override string Type { get; } = "Lingering";

        [JsonProperty] public Effect Effect { get; set; } = new NoEffect();
        [JsonProperty] public int CoolDown { get; set; } = 0;
        [JsonProperty] public int Duration { get; set; } = 1;
        [JsonProperty] public bool removedOnDeath { get; set; } = true;
        [JsonProperty] public string Text { get; set; } = "";
        [JsonIgnore] private ColossoFighter User;
        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            this.User = User;
            Target.LingeringEffects.Add(this);
            return new List<string>() { $"A {Effect.Type} Effect is lingering around {Target.Name}" };
        }

        public List<string> ApplyLingering(ColossoFighter Target)
        {

            var log = new List<string>();
            if (CoolDown > 0)
            {
                Console.WriteLine("On cooldown");
                CoolDown--;
                return log;
            }
            
            if (Duration >= 1)
            {
                Duration--;
                if (!Text.IsNullOrEmpty())
                {
                    log.Add(string.Format(Text, Target.Name));
                }
                log.AddRange(Effect.Apply(User, Target));
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