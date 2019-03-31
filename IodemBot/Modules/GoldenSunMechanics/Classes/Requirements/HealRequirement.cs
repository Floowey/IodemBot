using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class HealRequirement : IRequirement
    {
        public int apply(UserAccount user)
        {
            return user.BattleStats.HPhealed >= 999999 ? 1 : 0;
        }
    }
}