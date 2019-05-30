using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Extensions
{
    public static class IEnumerableExt
    {
        public static T Random<T>(this IEnumerable<T> ts)
        {
            return ts.ElementAt(Global.Random.Next(0, ts.Count()));
        }
    }
}