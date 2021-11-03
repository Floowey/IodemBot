using System.Collections.Generic;
using System.Linq;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.Extensions
{
    public static class ItemListExtension
    {
        public static Item GetItem(this IEnumerable<Item> list, ItemCategory cat)
        {
            return list?.FirstOrDefault(i => i.Category == cat);
        }

        public static bool HasItem(this IEnumerable<Item> list, ItemCategory cat)
        {
            return list.Any(i => i.Category == cat);
        }
    }
}