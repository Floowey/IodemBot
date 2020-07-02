using IodemBot.Modules.ColossoBattles;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class LingeringEffect : Effect
    {
        public override List<string> Apply(ColossoFighter User, ColossoFighter Target)
        {
            if (coolDown > 0)
            {
                coolDown--;
                return new List<string>();
            }
            else
            {
                return Effect.Apply(User, Target);
            }
        }

        public override string Type { get; } = "Lingering";

        [JsonProperty] private Effect Effect = new NoEffect();
        [JsonProperty] private int coolDown;
    }
}