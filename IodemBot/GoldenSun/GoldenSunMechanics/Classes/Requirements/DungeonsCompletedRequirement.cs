using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class DungeonsCompletedRequirement : IRequirement
    {
        public int Apply(UserAccount user)
        {
            if (user.ServerStats.DungeonsCompleted >= 150) //Berserker
            {
                return 4;
            }
            else if (user.ServerStats.DungeonsCompleted >= 100) //Barbarian
            {
                return 3;
            }
            else if (user.ServerStats.DungeonsCompleted >= 50) //Savage
            {
                return 2;
            }
            else if (user.ServerStats.DungeonsCompleted >= 25)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }
}
