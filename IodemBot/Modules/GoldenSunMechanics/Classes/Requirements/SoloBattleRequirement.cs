using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class SoloBattleRequirement : IRequirement
    {
        public int apply(UserAccount user)
        {
            return user.soloBattles >= 450 ? 2 : user.soloBattles >= 200 ? 1 : 0;
        }
    }
}
