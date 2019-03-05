using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class TeammatesRequirement : IRequirement
    {
        public int apply(UserAccount user)
        {
            return user.totalTeamMates >= 450 ? 2 : user.totalTeamMates >= 250 ? 1 : 0; 
        }
    }
}
