using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class UnlockedClassesRequirement : IRequirement
    {
        public int Apply(UserAccount user)
        {
            var nOfClasses = user.BonusClasses.Count;
            return nOfClasses switch
            {
                //War Adept
                >= 16 => 4,
                // Conjurer
                >= 12 => 3,
                // Enchanter
                >= 8 => 2,
                // Illusionist
                >= 4 => 1,
                _ => 0
            };
        }
    }
}