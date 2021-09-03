using System.Collections.Generic;
using System.Linq;
using IodemBot.Extensions;
using IodemBot.Modules.ColossoBattles;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class LingeringEffect : Effect
    {
        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            this.User = User;
            Target.LingeringEffects.Add(this);
            return new List<string>() { $"An Effect is lingering around {Target.Name}" };
        }

        public List<string> ApplyLingering(ColossoFighter Target)
        {
            var log = new List<string>();
            if (CoolDown > 0)
            {
                CoolDown--;
            }
            else if (Duration >= 1)
            {
                Duration--;
                if (!Text.IsNullOrEmpty())
                {
                    log.Add(string.Format(Text, Target.Name));
                }
                log.AddRange(Effect.Apply(User, Target));
            }
            return log;
        }

        public override string Type { get; } = "Lingering";

        [JsonProperty] private Effect Effect { get; set; } = new NoEffect();
        [JsonProperty] private int CoolDown { get; set; } = 0;
        [JsonProperty] private int Duration { get; set; } = 1;
        [JsonProperty] public bool removedOnDeath { get; set; } = true;
        [JsonProperty] private string Text { get; set; } = "";
        [JsonIgnore] private ColossoFighter User;
    }
}