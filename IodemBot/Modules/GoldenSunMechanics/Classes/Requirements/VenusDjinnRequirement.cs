using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class VenusDjinnRequirement : IRequirement
    {
        public int Apply(UserAccount user)
        {
            return user.DjinnPocket.djinn.OfElement(Element.Venus).Count() / 2;
        }
    }
}
