using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class DungeonsCompletedRequirement : IRequirement
    {
        public int Apply(UserAccount user)
        {
            return user.ServerStats.DungeonsCompleted switch
            {
                //Berserker
                >= 150 => 4,
                //Barbarian
                >= 100 => 3,
                //Savage
                >= 50 => 2,
                >= 25 => 1,
                _ => 0
            };
        }
    }
}