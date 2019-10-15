using IodemBot.Extensions;
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

        private static ShopStruct Shopstruct { get { return new ShopStruct() { shop = shop, lastReset = lastReset, restockmessage = restockMessage, shopkeeper = shopkeeper }; } }
        private static readonly int HoursForReset = 8;
        public static string shopkeeper;
        public static string restockMessage;
        private static string[] shopkeepers = { "armor shopkeeper2", "armor shopkeeper3", "champa shopkeeper", "item shopkeeper", "izumo shopkeeper", "weapon shopkeeper", "weapon shopkeeper2", "sunshine", "armor shopkeeper" };
        private static string[] restockMessages = { "Next shipment in:", "Next restock in:", "New Merchant in:" };

        private static readonly string shopLocation = "Resources/shop.json";
        private static readonly string itemLocation = "Resources/GoldenSun/items.json";

        public static TimeSpan TimeToNextReset
        {
            get
            {
                return lastReset.Add(new TimeSpan(HoursForReset, 0, 0)).Subtract(DateTime.Now);
            }
        }

        static ItemDatabase()
        {
            try
            {
                string json = File.ReadAllText(itemLocation);
                itemsDatabase = new Dictionary<string, Item>(
                    JsonConvert.DeserializeObject<Dictionary<string, Item>>(json),
                    StringComparer.OrdinalIgnoreCase);
                if (File.Exists(shopLocation))
                {
                    json = File.ReadAllText(shopLocation);
                    var s = JsonConvert.DeserializeObject<ShopStruct>(json);
                    shop = s.shop;
                    lastReset = s.lastReset;
                    shopkeeper = s.shopkeeper;
                    restockMessage = s.restockmessage;
                }

                var longestItem = itemsDatabase.Select(d => d.Value).OrderByDescending(d => $"{d.Icon} - {d.Name},".Length).First();
                Console.WriteLine($"{longestItem.Icon} - {longestItem.Name}, {$"{longestItem.Icon} - {longestItem.Name},".Length}");

                shop = GetShop();
            }
            catch (Exception e)
            {
                //Just for debugging.
                Console.WriteLine(e.ToString());
            }
        }

        private static void Save()
        {
            string json = JsonConvert.SerializeObject(Shopstruct, Formatting.Indented);
            File.WriteAllText(shopLocation, json);
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
            shop.Add(GetRandomItem(20, 0, RandomItemType.NonArtifact));

            shop.Add(GetRandomItem(30, 0, RandomItemType.Any));
            shop.Add(GetRandomItem(35, 0, RandomItemType.Any));
            shop.Add(GetRandomItem(40, 0, RandomItemType.Any));

            shop.Add(GetRandomItem(20, 0, RandomItemType.Artifact));
            shop.Add(GetRandomItem(40, 0, RandomItemType.Artifact));
            shop.Add(GetRandomItem(50, 0, RandomItemType.Artifact));

            shopkeeper = Sprites.GetImageFromName(shopkeepers.Random());

            restockMessage = restockMessages.Random();

            shop.Sort();
            if (shop.HasDuplicate || !(shop.HasItem(ItemCategory.UnderWear) || shop.HasItem(ItemCategory.Accessoire) || shop.HasItem(ItemCategory.FootWear)))
            {
                RandomizeShop();
            }
            else
            {
                lastReset = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, HoursForReset * (DateTime.Now.Hour / HoursForReset), 0, 0);
                Save();
            }
        }

        public static Inventory GetShop()
        {
            if (DateTime.Now.Subtract(lastReset).Hours >= HoursForReset)
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
            uint n = Math.Max((uint)(level + Math.Sqrt(bonus / 50)), 1);
            n = Math.Min(n, 100);
            var rate = 0.0007671 * Math.Pow(n, 2) - 0.1537 * n;
            var pow = Math.Pow(Math.E, rate);
            var loc = 15000 * 1.13 / (1 + 299 * pow);
            var scale = Math.Pow(n, 2.26);
            var shape = 0.1 - n / 200;
            var dist = new Accord.Statistics.Distributions.Univariate.GeneralizedParetoDistribution(loc, scale, shape);
            var value = dist.Generate();

            var allItems = itemsDatabase.Values.OrderByDescending(d => d.Price);
            var it = allItems.Where(i => i.Price <= value
                && (rt == RandomItemType.Artifact ? i.IsArtifact : true)
                && (rt == RandomItemType.NonArtifact ? !i.IsArtifact : true));

            Item price = allItems.OrderBy(i => i.Price).Take(10).Random();
            if (it != null && it.Count() >= 5)
            {
                price = it.TakeWhile(i => i.Price <= it.First().Price * 0.9).Union(it.Take(5)).Random();
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
                    if (s.EndsWith("(B)"))
                    {
                        var i = GetItem(s.Substring(0, s.Length - 3));
                        i.IsBroken = true;
                        items.Add(i);
                    }
                    else
                    {
                        items.Add(GetItem(s));
                    }
                }
            }

            return items;
        }

        internal struct ShopStruct
        {
            [JsonProperty] internal Inventory shop;
            [JsonProperty] internal DateTime lastReset;
            [JsonProperty] internal string shopkeeper;
            [JsonProperty] internal string restockmessage;
        }
    }
}