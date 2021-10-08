using System.Linq;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class MarsDjinnRequirement : IRequirement
    {
        public int Apply(UserAccount user)
        {
            return user.DjinnPocket.Djinn.OfElement(Element.Mars).Count(d => !d.IsEvent) / 2;
        }
    }
}
