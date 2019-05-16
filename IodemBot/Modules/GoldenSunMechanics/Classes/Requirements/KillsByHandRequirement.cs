using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class KillsByHandRequirement : IRequirement
    {
        public int apply(UserAccount user)
        {
            return user.BattleStats.KillsByHand >= 666 ? 1 : 0;
        }
    }
}