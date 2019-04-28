using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class ItemDatabase
    {
        private static Dictionary<string, Item> itemsDatabase = new Dictionary<string, Item>(StringComparer.OrdinalIgnoreCase);
        private static Inventory shop;
        private static DateTime lastReset;

        static ItemDatabase()
        {
            try
            {
                string json = File.ReadAllText("Resources/items.json");
                itemsDatabase = new Dictionary<string, Item>(
                    JsonConvert.DeserializeObject<Dictionary<string, Item>>(json),
                    StringComparer.OrdinalIgnoreCase);
                RandomizeShop();
            }
            catch (Exception e)
            {
                //Just for debugging.
                Console.WriteLine(e.ToString());
            }
        }

        public static void RandomizeShop()
        {
            if (shop == null)
            {
                shop = new Inventory(new List<string>(), new List<string>(), new List<string>());
            }

            shop.Clear();
            shop.Add(GetRandomItem(8, 0, RandomItemType.NonArtifact));
            shop.Add(GetRandomItem(20, 0, RandomItemType.NonArtifact));
            shop.Add(GetRandomItem(30, 0, RandomItemType.NonArtifact));

            shop.Add(GetRandomItem(30, 0, RandomItemType.Any));

            shop.Add(GetRandomItem(30, 0, RandomItemType.Artifact));
            shop.Add(GetRandomItem(40, 0, RandomItemType.Artifact));
            shop.Add(GetRandomItem(50, 0, RandomItemType.Artifact));
            lastReset = DateTime.Now;
        }

        public static Inventory GetShop()
        {
            if (DateTime.Now.Subtract(lastReset).Hours > 12)
            {
                RandomizeShop();
            }
            return shop;
        }

        public static Item GetItem(string itemName)
        {
            if (itemsDatabase.TryGetValue(itemName, out Item item))
            {
                return (Item)item.Clone();
            }

            return new Item() { Name = $"{itemName} NOT IMPLEMENTED!" };
        }

        public enum RandomItemType { Any, Artifact, NonArtifact }

        public static string GetRandomItem(uint level, double bonus = 0, RandomItemType rt = RandomItemType.Any)
        {
            uint n = (uint)(level + Math.Sqrt(bonus / 50));
            var dist = new Accord.Statistics.Distributions.Univariate.GeneralizedParetoDistribution(Math.Pow(n, 1.8), Math.Pow(n, 2.27), 0.1 - n / 200);
            var value = dist.Generate();

            var allItems = itemsDatabase.Values.OrderByDescending(d => d.Price);
            var it = allItems.Where(i => i.Price <= value
                && (rt == RandomItemType.Artifact ? i.IsArtifact : true)
                && (rt == RandomItemType.NonArtifact ? !i.IsArtifact : true));

            Item price = allItems.Last();
            if (it != null && it.Count() >= 5)
            {
                price = it.Take(5).ElementAt(Global.random.Next(0, 5));
            }

            return price.Name;
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