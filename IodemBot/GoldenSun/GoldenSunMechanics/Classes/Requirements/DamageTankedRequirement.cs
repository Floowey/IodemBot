using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class DamageTankedRequirement : IRequirement
    {
        public int Apply(UserAccount user)
        {
            return user.BattleStats.DamageTanked switch
            {
                >= 150000 => 2,
                >= 40000 => 1,
                _ => 0
            };
        }
    }
}