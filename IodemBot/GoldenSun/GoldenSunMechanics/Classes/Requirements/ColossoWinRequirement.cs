using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class ColossoWinRequirement : IRequirement
    {
        public int Apply(UserAccount user)
        {
            return user.ServerStats.ColossoWins switch
            {
                //Chaos Lord
                >= 1200 when user.ServerStats.EndlessStreak.Solo >= 27 => 5,
                //Berserker
                >= 800 when user.ServerStats.EndlessStreak.Solo >= 20 => 4,
                //Barbarian
                >= 500 => 3,
                //Savage
                >= 250 => 2,
                //Ruffian
                >= 50 => 1,
                _ => 0
            };
        }
    }
}