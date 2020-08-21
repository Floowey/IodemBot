using System.Linq;
using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class DjinnRequirement : IRequirement
    {
        public int Apply(UserAccount user)
        {
            var elements = new[] { Element.Venus, Element.Mars, Element.Jupiter, Element.Mercury };
            return (elements.Select(e => user.DjinnPocket.Djinn.Count(d => d.Element == e)).Min() + 1) / 2;
        }
    }
}
