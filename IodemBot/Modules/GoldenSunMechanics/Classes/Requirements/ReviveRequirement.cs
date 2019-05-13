using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class ReviveRequirement : IRequirement
    {
        public int apply(UserAccount user)
        {
            return user.BattleStats.Revives >= 200 ? 2 : user.BattleStats.Revives >= 120 ? 1 : 0;
        }
    }
}