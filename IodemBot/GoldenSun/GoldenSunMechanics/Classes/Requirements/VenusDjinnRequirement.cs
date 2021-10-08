using System.Linq;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class VenusDjinnRequirement : IRequirement
    {
        public int Apply(UserAccount user)
        {
            return user.DjinnPocket.Djinn.OfElement(Element.Venus).Count(d => !d.IsEvent) / 2;
        }
    }
}
