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
        private static readonly Dictionary<string, Item> ItemsDatabase = new(StringComparer.OrdinalIgnoreCase);
        private static Inventory _shop;
        private static DateTime _lastReset;

        private static readonly string[] Shopkeepers =
        {
            "armor shopkeeper2", "armor shopkeeper3", "champa shopkeeper", "item shopkeeper", "izumo shopkeeper",
            "weapon shopkeeper", "weapon shopkeeper2", "sunshine", "armor shopkeeper"
        };

        private static readonly string[] RestockMessages =
            {"Next shipment in:", "Next restock in:", "New Merchant in:"};

        private static readonly string ShopLocation = "Resources/shop.json";
        private static readonly string ItemLocation = "Resources/GoldenSun/items.json";

        public static readonly Dictionary<ChestQuality, RewardGenerator<ItemRarity>> ChestValues = new()
        {
            {
                ChestQuality.Wooden,
                new RewardGenerator<ItemRarity>(
                    new[] { ItemRarity.Common, ItemRarity.Uncommon }, new[] { 85, 15 })
            },
            {
                ChestQuality.Normal,
                new RewardGenerator<ItemRarity>(
                    new[] { ItemRarity.Common, ItemRarity.Uncommon }, new[] { 33, 67 })
            },
            {
                ChestQuality.Silver,
                new RewardGenerator<ItemRarity>(
                    new[] { ItemRarity.Uncommon, ItemRarity.Rare, ItemRarity.Legendary }, new[] { 40, 50, 10 })
            },
            {
                ChestQuality.Gold,
                new RewardGenerator<ItemRarity>(
                    new[] { ItemRarity.Rare, ItemRarity.Legendary, ItemRarity.Mythical }, new[] { 40, 50, 10 })
            },
            {
                ChestQuality.Adept,
                new RewardGenerator<ItemRarity>(
                    new[] { ItemRarity.Legendary, ItemRarity.Mythical }, new[] { 65, 35 })
            }
        };

        static ItemDatabase()
        {
            try
            {
                var json = File.ReadAllText(ItemLocation);
                ItemsDatabase = new Dictionary<string, Item>(
                    JsonConvert.DeserializeObject<Dictionary<string, Item>>(json),
                    StringComparer.OrdinalIgnoreCase);
                if (File.Exists(ShopLocation))
                {
                    json = File.ReadAllText(ShopLocation);
                    var s = JsonConvert.DeserializeObject<ShopStruct>(json);
                    _shop = s.Shop;
                    _lastReset = s.LastReset;
                    Shopkeeper = s.Shopkeeper;
                    RestockMessage = s.RestockMessage;
                }

                var longestItem = ItemsDatabase.Values
                    .OrderByDescending(d => $"{d.Icon.ToShortEmote()} - {d.Name},".Length).First();
                Console.WriteLine(
                    $"{longestItem.Icon.ToShortEmote()} - {longestItem.Name}, {$"{longestItem.Icon.ToShortEmote()} - {longestItem.Name},".Length}");

                _shop = GetShop();
            }
            catch (Exception e)
            {
                //Just for debugging.
                Console.WriteLine(e.ToString());
            }
        }

        private static ShopStruct Shopstruct => new()
        { Shop = _shop, LastReset = _lastReset, RestockMessage = RestockMessage, Shopkeeper = Shopkeeper };

        private static int HoursForReset { get; } =
            EventSchedule.CheckEvent("Shop")
                ? 6
                : 8;

        public static string Shopkeeper { get; set; }
        public static string RestockMessage { get; set; }

        public static TimeSpan TimeToNextReset =>
            _lastReset.Add(new TimeSpan(HoursForReset, 0, 0)).Subtract(DateTime.Now);

        private static void Save()
        {
            var json = JsonConvert.SerializeObject(Shopstruct, Formatting.Indented);
            File.WriteAllText(ShopLocation, json);
        }

        public static void RandomizeShop()
        {
            _shop ??= new Inventory();

            _shop.Clear();
            _shop.Add(GetRandomItem(ItemRarity.Common));
            _shop.Add(GetRandomItem(ItemRarity.Common));
            _shop.Add(GetRandomItem(ItemRarity.Uncommon));
            _shop.Add(GetRandomItem(new RewardGenerator<ItemRarity>(new[]
                        {ItemRarity.Uncommon, ItemRarity.Rare, ItemRarity.Legendary}, new[] { 60, 35, 5 })
                    .GenerateReward()
                )
            );
            _shop.Add(GetRandomItem(new RewardGenerator<ItemRarity>(new[]
                        {ItemRarity.Uncommon, ItemRarity.Rare, ItemRarity.Legendary}, new[] { 60, 35, 5 })
                    .GenerateReward()
                )
            );
            _shop.Add(GetRandomItem(new RewardGenerator<ItemRarity>(new[]
                        {ItemRarity.Rare, ItemRarity.Legendary, ItemRarity.Mythical}, new[] { 75, 24, 1 })
                    .GenerateReward()
                )
            );

            if (EventSchedule.CheckEvent("Shop"))
            {
                _shop.Add(GetRandomItem(new RewardGenerator<ItemRarity>(new[]
                        {ItemRarity.Rare, ItemRarity.Legendary, ItemRarity.Mythical}, new[] { 70, 24, 6 })
                    .GenerateReward()
                ));

                _shop.Add(GetRandomItem(new RewardGenerator<ItemRarity>(new[]
                        {ItemRarity.Rare, ItemRarity.Legendary, ItemRarity.Mythical}, new[] { 70, 24, 6 })
                    .GenerateReward()
                ));
            }

            Shopkeeper = Sprites.GetImageFromName(Shopkeepers.Random());

            RestockMessage = RestockMessages.Random();

            _shop.Sort();
            if (_shop.HasDuplicate || !(_shop.HasItem(ItemCategory.UnderWear) ||
                                        _shop.HasItem(ItemCategory.Accessory) || _shop.HasItem(ItemCategory.FootWear)))
            {
                RandomizeShop();
            }
            else
            {
                _lastReset = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                    HoursForReset * (DateTime.Now.Hour / HoursForReset), 0, 0);
                Save();
            }
        }

        public static Inventory GetShop()
        {
            if (DateTime.Now.Subtract(_lastReset).Hours >= HoursForReset) RandomizeShop();
            return _shop;
        }

        public static Item GetItem(string itemName)
        {
            var isBroken = itemName.Contains("(B)");
            var isAnimated = itemName.Contains("(A)");
            var isBoughtFromShop = itemName.Contains("(S)");
            var hasName = itemName.Contains('|');

            var justName = isBroken || isAnimated || hasName || isBoughtFromShop
                ? string.Concat(itemName.TakeWhile(c => !(c == '(' || c == '|')))
                : itemName;

            if (ItemsDatabase.TryGetValue(justName, out var item))
            {
                var i = (Item)item.Clone();
                if (hasName) i.Nickname = string.Concat(itemName.SkipWhile(c => !c.Equals('|')))[1..];
                i.IsAnimated = isAnimated;
                i.IsBroken = isBroken;
                i.IsBoughtFromShop = isBoughtFromShop;
                return i;
            }

            return new Item { Name = $"{itemName} NOT IMPLEMENTED!" };
        }

        public static bool TryGetItem(string itemName, out Item item)
        {
            item = GetItem(itemName);
            if (item.Name.ToLower().Contains("not implemented")) return false;
            return true;
        }

        public static IEnumerable<Item> GetAllItems()
        {
            return ItemsDatabase.Values.ToList().AsReadOnly();
        }

        public static Item GetRandomItem(ItemRarity rarity)
        {
            return ItemsDatabase.Values.Where(i => i.Rarity == rarity).Random();
        }

        public static List<Item> GetItems(IEnumerable<string> itemNames)
        {
            var items = new List<Item>();
            if (itemNames == null) return items;

            itemNames.ToList().ForEach(i =>
            {
                if (TryGetItem(i, out var item)) items.Add(item);
            });
            return items;
        }

        internal struct ShopStruct
        {
            [JsonProperty] internal Inventory Shop { get; set; }
            [JsonProperty] internal DateTime LastReset { get; set; }
            [JsonProperty] internal string Shopkeeper { get; set; }
            [JsonProperty] internal string RestockMessage { get; set; }
        }
    }
}