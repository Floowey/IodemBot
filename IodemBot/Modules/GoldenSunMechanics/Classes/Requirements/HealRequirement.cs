using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class HealRequirement : IRequirement
    {
        public int Apply(UserAccount user)
        {
            return user.BattleStats.HPhealed >= 555555 ? 1 : 0;
        }
    }
}