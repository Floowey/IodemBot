using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IodemBot.Extensions;
using Newtonsoft.Json; //using LiteDB;

namespace IodemBot.Modules.GoldenSunMechanics
{
    [JsonObject]
    public class Inventory : IEnumerable<Item>
    {
        public static readonly uint BaseInvSize = 30;

        public static readonly ItemType[] WarriorExclusive =
            {ItemType.LongSword, ItemType.Axe, ItemType.Shield, ItemType.Helmet, ItemType.HeavyArmor, ItemType.Greave};

        public static readonly ItemType[] MageExclusive =
            {ItemType.Staff, ItemType.Circlet, ItemType.Bow, ItemType.Robe, ItemType.Bracelet};

        public static readonly ChestQuality[] ChestQualities =
        {
            ChestQuality.Wooden, ChestQuality.Normal, ChestQuality.Silver, ChestQuality.Gold, ChestQuality.Adept,
            ChestQuality.Daily
        };

        public static readonly Dictionary<ItemCategory, string> WarriorIcons = new()
        {
            { ItemCategory.Weapon, "<:Swords:572526110357585920>" },
            { ItemCategory.ArmWear, "<:Shields:572526110118641664>" },
            { ItemCategory.HeadWear, "<:Helmets:572526110055858226>" },
            { ItemCategory.ChestWear, "<:Armors:572526109942611978>" },
            { ItemCategory.UnderWear, "<:Shirts:572526110173167616>" },
            { ItemCategory.FootWear, "<:Boots:572526109975904257>" },
            { ItemCategory.Accessory, "<:Rings:572526110060052482>" }
        };

        public static readonly Dictionary<ItemCategory, string> MageIcons = new()
        {
            { ItemCategory.Weapon, "<:Staves:572526110370168851>" },
            { ItemCategory.ArmWear, "<:Armlets:572526109908795402>" },
            { ItemCategory.HeadWear, "<:Circlets:572526110101864448>" },
            { ItemCategory.ChestWear, "<:Robes:572526110068441118>" },
            { ItemCategory.UnderWear, "<:Shirts:572526110173167616>" },
            { ItemCategory.FootWear, "<:Boots:572526109975904257>" },
            { ItemCategory.Accessory, "<:Rings:572526110060052482>" }
        };

        public static readonly uint RemoveCursedCost = 5000;

        public static readonly int[] DailyRewards = { 0, 0, 1, 1, 2 };

        /// <summary>
        ///     Serialized and deserialized a list of strings to their Item object equivalent
        /// </summary>
        public List<string> InvString
        {
            get => Inv.Count == 0 ? null : Inv?.Select(i => i.NameToSerialize).ToList();
            set => Inv = ItemDatabase.GetItems(value).ToList();
        }

        public List<string> WarriorGearString
        {
            get => WarriorGear.Count == 0 ? null : WarriorGear?.Select(i => i.Name).ToList();
            set => WarriorGear = value?.Select(i => GetItem(i)).ToList() ?? new List<Item>();
        }

        public List<string> MageGearString
        {
            get => MageGear.Count == 0 ? null : MageGear?.Select(i => i.Name).ToList();
            set => MageGear = value?.Select(i => GetItem(i)).ToList() ?? new List<Item>();
        }

        [JsonIgnore] private List<Item> Inv { get; set; } = new();

        [JsonIgnore] private List<Item> WarriorGear { get; set; } = new();

        [JsonIgnore] private List<Item> MageGear { get; set; } = new();

        public uint Coins { get; set; }
        public uint Upgrades { get; set; }

        internal uint MaxInvSize => BaseInvSize + 10 * Upgrades;

        public DateTime LastDailyChest { get; set; }
        public int DailiesInARow { get; set; }

        internal int Count => Inv.Count;

        public bool IsFull => Count >= MaxInvSize;

        internal bool HasDuplicate
        {
            get { return Inv.Any(i => Inv.Count(j => j.Name.Equals(i.Name)) > 1); }
        }

        public Dictionary<ChestQuality, uint> Chests { get; set; } = new()
        {
            { ChestQuality.Wooden, 0 },
            { ChestQuality.Normal, 0 },
            { ChestQuality.Silver, 0 },
            { ChestQuality.Gold, 0 },
            { ChestQuality.Adept, 0 },
            { ChestQuality.Daily, 0 }
        };

        public IEnumerator<Item> GetEnumerator()
        {
            return Inv.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Inv.GetEnumerator();
        }

        public bool HasAnyChests()
        {
            CheckDaily();
            return Chests.Values.Any(c => c > 0);
        }

        public ChestQuality NextChestQuality()
        {
            if (HasChest(ChestQuality.Daily))
                return ChestQuality.Daily;

            foreach (var cq in ChestQualities)
                if (HasChest(cq))
                    return cq;
            return ChestQuality.Daily;
        }

        public bool TryOpenChest(ChestQuality chestQuality, out Item item, uint level = 0)
        {
            item = null;
            if (IsFull) return false;

            if (!OpenChest(chestQuality)) return false;

            if (chestQuality == ChestQuality.Daily)
            {
                var rarity = (ItemRarity)(DailyRewards[DailiesInARow % DailyRewards.Length] + Math.Min(2, level / 33));
                item = ItemDatabase.GetRandomItem(rarity);
            }
            else
            {
                var rarity = ItemDatabase.ChestValues[chestQuality].GenerateReward();
                item = ItemDatabase.GetRandomItem(rarity);
            }

            return true;
        }

        public int NumberOfItemType(ItemType type)
        {
            return Inv.Count(i => i.ItemType == type);
        }

        public List<string> UnequipExclusiveTo(Element element)
        {
            var removed = new List<string>();
            foreach (var item in WarriorGear)
                if (item.ExclusiveTo != null && item.ExclusiveTo.Contains(element))
                    removed.Add(item.Name);
            foreach (var item in removed) Unequip(item);
            return removed;
        }

        public List<Item> GetGear(ArchType arch)
        {
            return arch == ArchType.Warrior ? WarriorGear : MageGear;
        }

        public bool OpenChest(ChestQuality chestQuality)
        {
            CheckDaily();
            Chests.TryGetValue(chestQuality, out var nOfChests);

            if (nOfChests == 0) return false;

            Chests.Remove(chestQuality);
            Chests.Add(chestQuality, nOfChests - 1);
            return true;
        }

        private void CheckDaily()
        {
            if (LastDailyChest.Date < DateTime.Now.Date && Chests[ChestQuality.Daily] == 0)
            {
                if ((DateTime.Now.Date - LastDailyChest.Date).TotalDays <= 1)
                    DailiesInARow++;
                else if (DateTime.Now.Date >= new DateTime(day: 1, month: 2, year: 2021))
                    DailiesInARow = 0;
                else
                    DailiesInARow++;
                AwardChest(ChestQuality.Daily);
                LastDailyChest = DateTime.Now;
            }
        }

        public void AwardChest(ChestQuality chestQuality)
        {
            Chests.TryGetValue(chestQuality, out var nOfChests);
            Chests.Remove(chestQuality);
            Chests.Add(chestQuality, ++nOfChests);
        }

        public string GetChestsToString()
        {
            CheckDaily();
            var s = new List<string>();
            foreach (var cq in ChestQualities)
                if (Chests[cq] > 0)
                    s.Add($"{Emotes.GetIcon(cq).ToShortEmote()}: {Chests[cq]}");
            return string.Join(" - ", s);
        }

        internal bool Remove(string item)
        {
            if (!HasItem(item)) return false;
            var it = Inv.Last(i => i.Name.Equals(item, StringComparison.InvariantCultureIgnoreCase));
            if (WarriorGear.Concat(MageGear).Any(i => i.Name.Equals(it.Name, StringComparison.CurrentCultureIgnoreCase)))
                if (Inv
                    .Count(i => string.Equals(i.Name, it.Name, StringComparison.InvariantCultureIgnoreCase)) == 1)
                    return false;

            Inv.Remove(it);
            return true;
        }

        public string InventoryToString(Detail detail = Detail.None)
        {
            if (Inv.Count == 0) return "empty";
            return detail switch
            {
                Detail.Names => string.Join(", ",
                    Inv.Select(i => $"{i.IconDisplay.ToShortEmote()} {i.Name}{(i.IsBroken ? " (Broken)" : "")}").ToArray()),
                Detail.NameAndPrice => string.Join("\n",
                    Inv.Select(i =>
                            $"{i.IconDisplay.ToShortEmote()} {i.Name} - {(Count <= 60 ? "<:coin:569836987767324672>" : "")}{i.Price}")
                        .ToArray()),
                _ => string.Join("", Inv.Select(i => i.IconDisplay.ToShortEmote()).ToArray())
            };
        }

        internal bool HasChest(ChestQuality cq)
        {
            CheckDaily();
            return Chests[cq] > 0;
        }

        internal bool HasItem(ItemCategory cat)
        {
            return Inv.HasItem(cat);
        }

        public void Clear()
        {
            Inv.Clear();
            WarriorGear.Clear();
            MageGear.Clear();
            Chests = new Dictionary<ChestQuality, uint>
            {
                {ChestQuality.Wooden, 0}, {ChestQuality.Normal, 0}, {ChestQuality.Silver, 0}, {ChestQuality.Gold, 0},
                {ChestQuality.Adept, 0}, {ChestQuality.Daily, 1}
            };
            Coins = 0;
            Upgrades = 0;
            //LastDailyChest = DateTime.Now.Subtract(new TimeSpan(1, 0, 0, 0));
        }

        internal Item GetItem(string item, Func<Item, bool> pre = null)
        {
            var i = Inv.FirstOrDefault(ii => pre?.Invoke(ii) ?? false);
            i ??= Inv.FirstOrDefault(d =>
                item.Equals(d.Itemname, StringComparison.CurrentCultureIgnoreCase) && d.Nickname.IsNullOrEmpty());
            i ??= Inv.FirstOrDefault(d => item.Equals(d.Itemname, StringComparison.CurrentCultureIgnoreCase));
            i ??= Inv.FirstOrDefault(d => item.Equals(d.Nickname, StringComparison.CurrentCultureIgnoreCase));
            return i;
        }

        public string GearToString(ArchType archType)
        {
            var s = new StringBuilder();
            var gear = GetGear(archType);

            var defaultIcons = archType == ArchType.Warrior ? WarriorIcons : MageIcons;

            foreach (var cat in Item.Equippables) s.Append(gear.GetItem(cat)?.IconDisplay.ToShortEmote() ?? defaultIcons[cat].ToShortEmote());

            return s.ToString();
        }

        internal bool Repair(string item)
        {
            var it = GetItem(item, i => i.IsBroken);
            if (it != null && RemoveBalance(it.SellValue))
            {
                it.IsBroken = false;
                return true;
            }

            return false;
        }

        internal bool Rename(string item, string newname = null)
        {
            if (!HasItem(item)) return false;

            var it = GetItem(item);

            it.Nickname = newname ?? "";
            return true;
        }

        internal bool Polish(string item)
        {
            if (!HasItem(item)) return false;

            var it = GetItem(item);
            if (!it.CanBeAnimated) return false;

            if (!RemoveBalance(it.Price * 10)) return false;

            it.IsAnimated = true;
            return true;
        }

        public bool HasItem(string item)
        {
            return Inv.Any(s =>
                s.Name.Equals(item, StringComparison.CurrentCultureIgnoreCase) ||
                s.Itemname.Equals(item, StringComparison.CurrentCultureIgnoreCase));
        }

        public bool Equip(string item, ArchType archType)
        {
            if (!HasItem(item)) return false;

            var i = GetItem(item);
            if (!i.IsEquippable) return false;

            if (!i.IsEquippableBy(archType)) return false;

            var gear = GetGear(archType);

            var g = gear.GetItem(i.Category);
            if (g != null)
            {
                if (g.IsCursed)
                    return false;
                gear.Remove(g);
            }

            gear.Add(i);
            return true;
        }

        public bool Unequip(string item)
        {
            var it = GetItem(item);
            if (it == null ||
                !WarriorGear.Any(i => i.Name.Equals(it.Name, StringComparison.CurrentCultureIgnoreCase)) &&
                !MageGear.Any(i => i.Name.Equals(it.Name, StringComparison.CurrentCultureIgnoreCase)))
                return false;

            if (it.IsCursed) return false;

            WarriorGear.RemoveAll(i => i.Name.Equals(it.Name, StringComparison.CurrentCultureIgnoreCase));
            MageGear.RemoveAll(i => i.Name.Equals(it.Name, StringComparison.CurrentCultureIgnoreCase));
            return true;
        }

        public List<Item> CursedGear()
        {
            return Inv.Where(i => i.IsCursed).ToList();
        }

        public bool RemoveCursedEquipment()
        {
            if (!MageGear.Any(w => w.IsCursed) && !WarriorGear.Any(w => w.IsCursed)) return false;

            if (!RemoveBalance(RemoveCursedCost)) return false;

            WarriorGear.RemoveAll(i => i.IsCursed);
            MageGear.RemoveAll(i => i.IsCursed);

            return true;
        }

        public bool Add(string item)
        {
            var i = ItemDatabase.GetItem(item);
            if (i.Name.Contains("NOT IMPLEMENTED!")) return false;

            return Add(i);
        }

        public bool Add(Item item)
        {
            if (IsFull) return false;
            Inv.Add(item);
            return true;
        }

        public bool Buy(string item)
        {
            var i = ItemDatabase.GetItem(item);
            if (i.Name.Contains("NOT IMPLEMENTED!")) return false;
            if (IsFull) return false;

            if (!RemoveBalance(i.Price)) return false;

            return Add(i.Name);
        }

        public void Sort()
        {
            Inv = Inv.OrderByDescending(d => WarriorGear.Any(e => e.Name.Equals(d.Name)))
                .ThenByDescending(d =>
                    MageGear.Any(e => e.Name.Equals(d.Name)) && !WarriorGear.Any(e => e.Name.Equals(d.Name)))
                .ThenBy(d => d.ItemType)
                .ThenBy(d => d.Name)
                .ToList();
        }

        public bool Sell(string item)
        {
            if (!HasItem(item)) return false;
            var it = GetItem(item);
            if (WarriorGear.Concat(MageGear).Any(i => i.Name.Equals(it.Name, StringComparison.CurrentCultureIgnoreCase)))
                if (Inv
                    .Count(i => string.Equals(i.Name, it.Name, StringComparison.InvariantCultureIgnoreCase)) == 1)
                    return false;

            Inv.Remove(it);
            Coins += it.SellValue;
            return true;
        }

        public void AddBalance(uint amount)
        {
            Coins += amount;
        }

        public bool HasBalance(uint amount)
        {
            return amount <= Coins;
        }

        public bool RemoveBalance(uint amount)
        {
            if (!HasBalance(amount)) return false;

            Coins -= amount;
            return true;
        }
    }
}