using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class CursedItemsRequirement : IRequirement
    {
        public int apply(UserAccount user)
        {
            return user.Inv.GetGear(ArchType.Mage).Count(i => i.IsCursed);
        }
    }
}
