using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace IodemBot.Extensions
{
    public static class ListExtension
    {
        private static readonly RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
        private static Random rng = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        //public static void Shuffle<T>(this IList<T> list)
        //{
        //    list = list.OrderBy(i => Global.RandomNumber(0, 10000)).ToList();
        //}
    }
}