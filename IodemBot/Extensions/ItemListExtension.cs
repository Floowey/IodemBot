using System.Collections.Generic;
using System.Linq;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.Extensions
{
    public static class ItemListExtension
    {
        public static Item GetItem(this List<Item> list, ItemCategory cat)
        {
            if (list == null)
            {
                return null;
            }

            return list.Where(i => i.Category == cat).FirstOrDefault();
        }

        public static bool HasItem(this List<Item> list, ItemCategory cat)
        {
            return list.Any(i => i.Category == cat);
        }
    }
}