using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class ReviveRequirement : IRequirement
    {
        public int apply(UserAccount user)
        {
            return user.BattleStats.Revives >= 111 ? 2 : user.BattleStats.Revives >= 66 ? 1 : 0;
        }
    }
}