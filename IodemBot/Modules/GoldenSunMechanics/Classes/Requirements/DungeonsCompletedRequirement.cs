using System;
using System.Collections.Generic;
using System.Text;
using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class DungeonsCompletedRequirement : IRequirement
    {
        public int apply(UserAccount user)
        {
            if (user.ServerStats.DungeonsCompleted >= 200) //Berserker
            {
                return 4;
            }
            else if (user.ServerStats.DungeonsCompleted >= 123) //Barbarian
            {
                return 3;
            }
            else if (user.ServerStats.DungeonsCompleted >= 66) //Savage
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
