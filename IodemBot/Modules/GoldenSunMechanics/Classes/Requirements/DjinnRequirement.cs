using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IodemBot.Core.UserManagement;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class DjinnRequirement : IRequirement
    {
        public int apply(UserAccount user)
        {
            var elements = new[] { Element.Venus, Element.Mars, Element.Jupiter, Element.Mercury };
            return (elements.Select(e => user.DjinnPocket.djinn.Count(d => d.Element == e)).Min()+1) / 2;
        }
    }
}
