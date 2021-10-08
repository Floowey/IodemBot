using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class TeammatesRequirement : IRequirement
    {
        public int Apply(UserAccount user)
        {
            return user.BattleStats.TotalTeamMates >= 400 ? 2 : user.BattleStats.TotalTeamMates >= 200 ? 1 : 0;
        }
    }
}