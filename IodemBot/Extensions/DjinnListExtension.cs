using System.Collections.Generic;
using System.Linq;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.Extensions
{
    public static class DjinnListExtension
    {
        public static IEnumerable<Djinn> OfElement(this IEnumerable<Djinn> djinn, Element el)
        {
            if (djinn == null) return null;
            return djinn.Where(d => d.Element == el);
        }

        public static IEnumerable<Djinn> OfElement(this IEnumerable<Djinn> djinn, Element[] el)
        {
            if (djinn == null) return null;
            return djinn.Where(d => el.Contains(d.Element));
        }

        public static string GetDisplay(this IEnumerable<Djinn> djinn, DjinnDetail detail)
        {
            var seperator = detail == DjinnDetail.Names ? ", " : "";
            var s = string.Join(seperator, djinn.Select(d => $"{d.Emote}{(detail == DjinnDetail.Names ? $" {d.Name}" : "")}"));

            return s.IsNullOrEmpty() ? "-" : s;
        }
    }
}