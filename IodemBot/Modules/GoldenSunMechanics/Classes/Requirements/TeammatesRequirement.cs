using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class TeammatesRequirement : IRequirement
    {
        public int apply(UserAccount user)
        {
            return user.BattleStats.TotalTeamMates >= 450 ? 2 : user.BattleStats.TotalTeamMates >= 250 ? 1 : 0;
        }
    }
}