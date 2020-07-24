using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class CursedItemsRequirement : IRequirement
    {
        public int Apply(UserAccount user)
        {
            var cursedGearWorn = user.Inv.GetGear(ArchType.Mage).Count(it => it.IsCursed);
            if (cursedGearWorn == 0) return 0;

            return user.Inv.CursedGear().Select(i => i.Itemname).Distinct().Count()/2+cursedGearWorn;
        }
    }
}
