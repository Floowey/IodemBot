﻿using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class UnlockedClassesRequirement : IRequirement
    {
        public int Apply(UserAccount user)
        {
            var nOfClasses = user.BonusClasses.Count;
            if (nOfClasses >= 16) //War Adept
            {
                return 4;
            }
            else if (nOfClasses >= 12) // Conjurer
            {
                return 3;
            }
            else if (nOfClasses >= 8) // Enchanter
            {
                return 2;
            }
            else if (nOfClasses >= 4) // Illusionist
            {
                return 1;
            }
            else // Page
            {
                return 0;
            }
        }
    }
}