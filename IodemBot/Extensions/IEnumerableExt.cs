using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Extensions
{
    public static class EnumerableExt
    {
        public static T Random<T>(this IEnumerable<T> ts)
        {
            return !ts.Any() ? default : ts.ElementAt(Global.RandomNumber(0, ts.Count()));
        }
    }
}