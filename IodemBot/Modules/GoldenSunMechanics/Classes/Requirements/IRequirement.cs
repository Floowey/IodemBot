using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public interface IRequirement
    {
        int Apply(UserAccount user);
    }
}