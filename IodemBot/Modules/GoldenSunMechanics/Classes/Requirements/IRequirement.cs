using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public interface IRequirement
    {
        int apply(UserAccount user);
    }
}