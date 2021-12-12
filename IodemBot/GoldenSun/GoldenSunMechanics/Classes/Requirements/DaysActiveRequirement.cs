using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class DaysActiveRequirement : IRequirement
    {
        public int Apply(UserAccount user)
        {
            return user.ServerStats.UniqueDaysActive switch
            {
                //Wizard
                >= 70 => 5,
                //Sage
                >= 45 => 4,
                //Savant
                >= 30 => 3,
                //Scholar
                >= 14 => 2,
                //Elder
                >= 7 => 1,
                _ => 0
            };
        }
    }
}