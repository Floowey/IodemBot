using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;

namespace IodemBot.Modules.GoldenSunMechanics
{
    class JupiterDjinnRequirement : IRequirement
    {
        public int apply(UserAccount user)
        {
            return user.DjinnPocket.djinn.OfElement(Element.Jupiter).Count() / 2;
        }
    }
}
