using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class KillsByHandRequirement : IRequirement
    {
        public int apply(UserAccount user)
        {
            return user.BattleStats.killsByHand >= 666 ? 1 : 0;
        }
    }
}