using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class HealRequirement : IRequirement
    {
        public int apply(UserAccount user)
        {
            return user.BattleStats.HPhealed >= 999999 ? 1 : 0;
        }
    }
}
