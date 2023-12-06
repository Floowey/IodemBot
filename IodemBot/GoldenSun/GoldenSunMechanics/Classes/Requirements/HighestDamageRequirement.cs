using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class HighestDamageRequirement : IRequirement
    {
        public int Apply(UserAccount user)
        {
            return user.BattleStats.HighestDamage >= 1500 ? 2 : user.BattleStats.HighestDamage >= 1000 ? 1 : 0;
        }
    }
}