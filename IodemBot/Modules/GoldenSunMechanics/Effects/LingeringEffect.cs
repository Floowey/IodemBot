using System.Collections.Generic;
using IodemBot.Modules.ColossoBattles;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class LingeringEffect : Effect
    {
        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            if (CoolDown > 0)
            {
                CoolDown--;
                return new List<string>();
            }
            else
            {
                return Effect.Apply(User, Target);
            }
        }

        public override string Type { get; } = "Lingering";

        [JsonProperty] private Effect Effect { get; set; } = new NoEffect();
        [JsonProperty] private int CoolDown { get; set; }
    }
}