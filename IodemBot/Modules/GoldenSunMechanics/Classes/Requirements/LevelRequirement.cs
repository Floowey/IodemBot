using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class LevelRequirement : IRequirement
    {
        public int apply(UserAccount user)
        {
            return (int)(user.LevelNumber / 12);
        }
    }
}