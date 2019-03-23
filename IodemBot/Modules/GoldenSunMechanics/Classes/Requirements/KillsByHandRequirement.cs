using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class KillsByHandRequirement : IRequirement
    {
        public int apply(UserAccount user)
        {
            return user.BattleStats.killsByHand >= 666 ? 1 : 0;
        }
    }
}
