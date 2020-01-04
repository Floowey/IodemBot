using IodemBot.Core.UserManagement;
using JsonSubTypes;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    [JsonConverter(typeof(JsonSubtypes))]
    [JsonSubtypes.FallBackSubType(typeof(DefaultReward))]
    public abstract class Rewardable
    {
        public int Weight { get; set; } = 1;

        public abstract string Award(UserAccount userAccount);
    }
}