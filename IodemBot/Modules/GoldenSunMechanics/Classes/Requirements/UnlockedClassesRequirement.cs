using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class UnlockedClassesRequirement : IRequirement
    {
        public int apply(UserAccount user)
        {
            var nOfClasses = user.BonusClasses.Length;
            if (nOfClasses >= 14) //War Adept
            {
                return 4;
            }
            else if (nOfClasses >= 9) // Conjurer
            {
                return 3;
            }
            else if (nOfClasses >= 6) // Enchanter
            {
                return 2;
            }
            else if (nOfClasses >= 3) // Illusionist
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