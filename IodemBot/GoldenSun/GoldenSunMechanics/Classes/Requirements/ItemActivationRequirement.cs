using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class ItemActivationRequirement : IRequirement
    {
        public int Apply(UserAccount user)
        {
            var itemActivations = user.BattleStats.ItemActivations;
            return itemActivations switch
            {
                //Crystalmancer
                >= 100 => 2,
                // Jeweller
                >= 40 => 1,
                //Prospector
                _ => 0
            };
        }
    }
}