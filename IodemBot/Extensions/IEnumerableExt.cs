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

        public static List<List<T>> ChunkBy<T>(this IEnumerable<T> source, int chunkSize)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }
    }
}