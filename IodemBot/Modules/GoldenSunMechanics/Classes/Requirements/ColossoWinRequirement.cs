using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class ColossoWinRequirement : IRequirement
    {
        public int apply(UserAccount user)
        {
            if(user.ColossoWins >= 800 && user.ColossoHighestStreak >= 25) //Chaos Lord
            {
                return 5;
            } else if (user.ColossoWins >= 500 && user.ColossoHighestStreak >= 10) //Berserker
            {
                return 4;
            } else if (user.ColossoWins >= 300) //Barbarian
            {
                return 3;
            } else if (user.ColossoWins >= 150) //Savage
            {
                return 2;
            } else if (user.ColossoWins >= 50) //Ruffian
            {
                return 1;
            } else // Brute
            {
                return 0;
            }
        }
    }
}
