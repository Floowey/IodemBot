using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class HighestDamageRequirement : IRequirement
    {
        public int Apply(UserAccount user)
        {
            return user.BattleStats.HighestDamage >= 900 ? 2 : user.BattleStats.HighestDamage >= 600 ? 1 : 0;
        }
    }
}