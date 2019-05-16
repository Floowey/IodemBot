using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class DamageRequirement : IRequirement
    {
        public int apply(UserAccount user)
        {
            return user.BattleStats.DamageDealt >= 3333333 ? 2 : user.BattleStats.DamageDealt >= 999999 ? 1 : 0;
        }
    }
}