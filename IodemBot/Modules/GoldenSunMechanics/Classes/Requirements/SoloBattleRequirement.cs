using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class SoloBattleRequirement : IRequirement
    {
        public int apply(UserAccount user)
        {
            return user.BattleStats.SoloBattles >= 400 ? 2 : user.BattleStats.SoloBattles >= 200 ? 1 : 0;
        }
    }
}