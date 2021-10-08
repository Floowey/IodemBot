using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class ColossoWinRequirement : IRequirement
    {
        public int Apply(UserAccount user)
        {
            if (user.ServerStats.ColossoWins >= 1200 && user.ServerStats.EndlessStreak.Solo >= 27) //Chaos Lord
            {
                return 5;
            }
            else if (user.ServerStats.ColossoWins >= 800 && user.ServerStats.EndlessStreak.Solo >= 20) //Berserker
            {
                return 4;
            }
            else if (user.ServerStats.ColossoWins >= 500) //Barbarian
            {
                return 3;
            }
            else if (user.ServerStats.ColossoWins >= 250) //Savage
            {
                return 2;
            }
            else if (user.ServerStats.ColossoWins >= 50) //Ruffian
            {
                return 1;
            }
            else // Brute
            {
                return 0;
            }
        }
    }
}