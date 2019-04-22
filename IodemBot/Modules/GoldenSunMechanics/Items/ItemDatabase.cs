using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class ItemDatabase
    {
        private static Dictionary<string, Item> itemsDatabase = new Dictionary<string, Item>(StringComparer.OrdinalIgnoreCase);

        static ItemDatabase()
        {
            try
            {
                string json = File.ReadAllText("Resources/items.json");
                itemsDatabase = new Dictionary<string, Item>(
                    JsonConvert.DeserializeObject<Dictionary<string, Item>>(json),
                    StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception e)
            {
                //Just for debugging.
                Console.WriteLine(e.ToString());
            }

        }

        public static Item GetItem(string itemName)
        {
            if (itemsDatabase.TryGetValue(itemName, out Item item))
            {
                return (Item)item.Clone();
            }

            return new Item() { Name = "NOT IMPLEMENTED!" };
        }

        public static List<Item> GetItems(IEnumerable<string> itemNames)
        {
            List<Item> items = new List<Item>();
            if (itemNames == null)
            {
                return items;
            }

            if (itemNames.Count() > 0)
            {
                foreach (var s in itemNames)
                {
                    items.Add(GetItem(s));
                }
            }

            return items;
        }
    }

   
}
