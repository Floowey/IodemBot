using IodemBot.Modules.GoldenSunMechanics;
using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Extensions
{
    public static class DjinnListExtension
    {
        public static IEnumerable<Djinn> OfElement(this IEnumerable<Djinn> djinn, Element el)
        {
            return djinn.Where(d => d.Element == el);
        }
    }
}