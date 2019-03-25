using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class ColossoWinRequirement : IRequirement
    {
        public int apply(UserAccount user)
        {
            if (user.ServerStats.ColossoWins >= 800 && user.ServerStats.ColossoHighestStreak >= 25) //Chaos Lord
            {
                return 5;
            }
            else if (user.ServerStats.ColossoWins >= 500 && user.ServerStats.ColossoHighestStreak >= 10) //Berserker
            {
                return 4;
            }
            else if (user.ServerStats.ColossoWins >= 300) //Barbarian
            {
                return 3;
            }
            else if (user.ServerStats.ColossoWins >= 150) //Savage
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