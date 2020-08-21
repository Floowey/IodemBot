using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Extensions
{
    public static class IEnumerableExt
    {
        public static T Random<T>(this IEnumerable<T> ts)
        {
            if (ts.Count() == 0)
            {
                return default;
            }

            return ts.ElementAt(Global.RandomNumber(0, ts.Count()));
        }
    }
}