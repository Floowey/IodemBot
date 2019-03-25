using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class TeammatesRequirement : IRequirement
    {
        public int apply(UserAccount user)
        {
            return user.BattleStats.totalTeamMates >= 450 ? 2 : user.BattleStats.totalTeamMates >= 250 ? 1 : 0;
        }
    }
}