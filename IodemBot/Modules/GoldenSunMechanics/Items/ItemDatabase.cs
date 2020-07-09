using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics.RewardSystem;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class ItemDatabase
    {
        private static readonly Dictionary<string, Item> itemsDatabase = new Dictionary<string, Item>(StringComparer.OrdinalIgnoreCase);
        private static Inventory shop;
        private static DateTime lastReset;

        private static ShopStruct Shopstruct { get { return new ShopStruct() { shop = shop, lastReset = lastReset, restockmessage = restockMessage, shopkeeper = shopkeeper }; } }
        private static readonly int HoursForReset = 8;
        public static string shopkeeper;
        public static string restockMessage;
        private static readonly string[] shopkeepers = { "armor shopkeeper2", "armor shopkeeper3", "champa shopkeeper", "item shopkeeper", "izumo shopkeeper", "weapon shopkeeper", "weapon shopkeeper2", "sunshine", "armor shopkeeper" };
        private static readonly string[] restockMessages = { "Next shipment in:", "Next restock in:", "New Merchant in:" };

        private static readonly string shopLocation = "Resources/shop.json";
        private static readonly string itemLocation = "Resources/GoldenSun/items.json";

        public static Dictionary<ChestQuality, RewardGenerator<ItemRarity>> ChestValues = new Dictionary<ChestQuality, RewardGenerator<ItemRarity>>() {
                {ChestQuality.Wooden, new RewardGenerator<ItemRarity>(
                    new []{ItemRarity.Common }, new []{ 100 })
                },
                {ChestQuality.Normal, new RewardGenerator<ItemRarity>(
                    new []{ItemRarity.Common, ItemRarity.Uncommon }, new []{ 60, 40})
                },
                {ChestQuality.Silver, new RewardGenerator<ItemRarity>(
                    new []{ItemRarity.Uncommon, ItemRarity.Rare, ItemRarity.Legendary}, new []{ 40, 55, 5})
                },
                {ChestQuality.Gold, new RewardGenerator<ItemRarity>(
                    new []{ItemRarity.Rare, ItemRarity.Legendary, ItemRarity.Mythical}, new []{ 59, 40, 1})
                },
                {ChestQuality.Adept, new RewardGenerator<ItemRarity>(
                    new []{ItemRarity.Rare, ItemRarity.Legendary, ItemRarity.Mythical}, new []{ 30, 65, 5})
                }
            };
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
                shop = new Inventory();
            }

            shop.Clear();
            shop.Add(GetRandomItem(ItemRarity.Common));
            shop.Add(GetRandomItem(ItemRarity.Common));
            shop.Add(GetRandomItem(ItemRarity.Common));
            shop.Add(GetRandomItem(ItemRarity.Uncommon));
            shop.Add(GetRandomItem(new RewardGenerator<ItemRarity>(new[]
                { ItemRarity.Uncommon, ItemRarity.Rare, ItemRarity.Legendary}, new[] { 60, 35, 5 }).GenerateReward()
                )
            );
            shop.Add(GetRandomItem(new RewardGenerator<ItemRarity>(new[]
                { ItemRarity.Rare, ItemRarity.Legendary, ItemRarity.Mythical}, new[] { 75, 24, 1 }).GenerateReward()
                )
            );
            //shop.Add(GetRandomItem(8, RandomItemType.NonArtifact));
            //shop.Add(GetRandomItem(12, RandomItemType.NonArtifact));
            ////shop.Add(GetRandomItem98(18, RandomItemType.NonArtifact));

            //shop.Add(GetRandomItem(25, RandomItemType.Any));
            ////shop.Add(GetRandomItem(28, 0, RandomItemType.Any));
            //shop.Add(GetRandomItem(32, RandomItemType.Any));

            //shop.Add(GetRandomItem(15, RandomItemType.Artifact));
            ////shop.Add(GetRandomItem(24, RandomItemType.Artifact));
            //shop.Add(GetRandomItem(38, RandomItemType.Artifact));

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
            var isBroken = itemName.Contains("(B)");
            var isAnimated = itemName.Contains("(A)");
            var hasName = itemName.Contains("|");

            var justName = (isBroken || isAnimated || hasName) ? string.Concat(itemName.TakeWhile(c => !(c == '(' || c == '|'))) : itemName;

            if (itemsDatabase.TryGetValue(justName, out Item item))
            {
                var i = (Item)item.Clone();
                if (hasName)
                {
                    i.Nickname = string.Concat(itemName.SkipWhile(c => !c.Equals('|'))).Substring(1);
                }
                i.IsAnimated = isAnimated;
                i.IsBroken = isBroken;
                return i;
            }

            return new Item() { Name = $"{itemName} NOT IMPLEMENTED!" };
        }

        public static bool TryGetItem(string ItemName, out Item item)
        {
            item = GetItem(ItemName);
            if (item.Name.ToLower().Contains("not implemented"))
            {
                return false;
            }
            return true;
        }

        public static string GetRandomItem(ItemRarity rarity)
        {
            return itemsDatabase.Values.Where(i => i.Rarity == rarity).Random().Name;
        }

        public static string GetRandomItem(uint level, RandomItemType rt = RandomItemType.Any)
        {
            uint n = Math.Max(level, 1);
            n = Math.Min(n, 100);
            var rate = 0.0007671 * Math.Pow(n, 2) - 0.1537 * n;
            var pow = Math.Pow(Math.E, rate);
            var loc = 11000 * 1.13 / (1 + 299 * pow);
            var scale = Math.Pow(n, 2.2055);
            var shape = 0.1 - n / 200;
            var dist = new Accord.Statistics.Distributions.Univariate.GeneralizedParetoDistribution(loc, scale, shape);
            var value = dist.Generate();
            foreach (int i in new[] { 5000, 10000, 15000, 20000, 25000, 30000, 35000 })
            {
                Console.WriteLine($"{i}: {(1 - dist.DistributionFunction(i)) * 100}%");
            }
            var allItems = itemsDatabase.Values.OrderByDescending(d => d.Price);
            var it = allItems.Where(i => i.Price <= value
                && (rt != RandomItemType.Artifact || i.IsArtifact)
                && (rt != RandomItemType.NonArtifact || !i.IsArtifact));

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

            itemNames.ToList().ForEach(i =>
            {
                if (TryGetItem(i, out var item))
                {
                    items.Add(item);
                }
            });
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