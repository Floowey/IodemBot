using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class PPUsedRequirement : IRequirement
    {
        public int Apply(UserAccount user)
        {
            return user.BattleStats.PPUsed >= 7000 ? 2 : user.BattleStats.PPUsed >= 3000 ? 1 : 0;
        }
    }
}