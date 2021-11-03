using System.Linq;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class MercuryDjinnRequirement : IRequirement
    {
        public int Apply(UserAccount user)
        {
            return user.DjinnPocket.Djinn.OfElement(Element.Mercury).Count(d => !d.IsEvent) / 2;
        }
    }
}