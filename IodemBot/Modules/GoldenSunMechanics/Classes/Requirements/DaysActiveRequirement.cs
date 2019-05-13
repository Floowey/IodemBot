using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class DaysActiveRequirement : IRequirement
    {
        public int apply(UserAccount user)
        {
            if (user.ServerStats.UniqueDaysActive >= 70) //Wizard
            {
                return 5;
            }
            else if (user.ServerStats.UniqueDaysActive >= 45) //Sage
            {
                return 4;
            }
            else if (user.ServerStats.UniqueDaysActive >= 30) //Savant
            {
                return 3;
            }
            else if (user.ServerStats.UniqueDaysActive >= 14) //Scholar
            {
                return 2;
            }
            else if (user.ServerStats.UniqueDaysActive >= 7) //Elder
            {
                return 1;
            }
            else // Hermit
            {
                return 0;
            }
        }
    }
}