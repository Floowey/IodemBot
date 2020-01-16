using IodemBot.Core.UserManagement;
using JsonSubTypes;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace IodemBot.Modules.GoldenSunMechanics
{
    [JsonConverter(typeof(JsonSubtypes))]
    [JsonSubtypes.FallBackSubType(typeof(DefaultReward))]
    public abstract class Rewardable
    {
        public int Weight { get; set; } = 1;
        public string Tag { get; set; } = "";
        public List<string> RequireTag { get; set; } = new List<string>();
        public int Obtainable { get; set; } = 0;

        public abstract string Award(UserAccount userAccount);
    }
}