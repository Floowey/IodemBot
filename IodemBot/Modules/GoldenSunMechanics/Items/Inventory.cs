﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class Inventory
    {
        public static readonly uint BaseInvSize = 30;
        private static readonly ItemType[] WarriorExclusive = { ItemType.LongSword, ItemType.Axe, ItemType.Shield, ItemType.Helmet, ItemType.HeavyArmor, ItemType.Greave };
        private static readonly ItemType[] MageExclusive = { ItemType.Staff, ItemType.Circlet, ItemType.Bow, ItemType.Robe, ItemType.Bracelet };

        public static readonly ChestQuality[] chestQualities = { ChestQuality.Wooden, ChestQuality.Normal, ChestQuality.Silver, ChestQuality.Gold, ChestQuality.Adept, ChestQuality.Daily };

        public static readonly Dictionary<ChestQuality, string> ChestIcons = new Dictionary<ChestQuality, string>()
        {
            {ChestQuality.Wooden, "<:wooden_chest:570332670576295986>" },
            {ChestQuality.Normal, "<:chest:570332670442078219>" },
            {ChestQuality.Silver, "<:silver_chest:570332670391877678>" },
            {ChestQuality.Gold, "<:gold_chest:570332670530158593>" },
            {ChestQuality.Adept, "<:adept_chest:570332670329094146>" },
            {ChestQuality.Daily, "<:daily_chest:570332670605787157>" }
        };

        internal static readonly Dictionary<ItemCategory, string> WarriorIcons = new Dictionary<ItemCategory, string>()
        {
            {ItemCategory.Weapon, "<:Swords:572526110357585920>" },
            {ItemCategory.ArmWear, "<:Shields:572526110118641664>" },
            {ItemCategory.HeadWear,"<:Helmets:572526110055858226>" },
            {ItemCategory.ChestWear, "<:Armors:572526109942611978>" },
            {ItemCategory.UnderWear, "<:Shirts:572526110173167616>" },
            {ItemCategory.FootWear,"<:Boots:572526109975904257>" },
            {ItemCategory.Accessoire, "<:Rings:572526110060052482>"}
        };

        internal static readonly Dictionary<ItemCategory, string> MageIcons = new Dictionary<ItemCategory, string>()
        {
            {ItemCategory.Weapon,  "<:Staves:572526110370168851>" },
            {ItemCategory.ArmWear, "<:Armlets:572526109908795402>"},
            {ItemCategory.HeadWear,"<:Circlets:572526110101864448>" },
            {ItemCategory.ChestWear, "<:Robes:572526110068441118>" },
            {ItemCategory.UnderWear, "<:Shirts:572526110173167616>" },
            {ItemCategory.FootWear,"<:Boots:572526109975904257>" },
            {ItemCategory.Accessoire, "<:Rings:572526110060052482>"}
        };

        [JsonProperty] private List<string> InvString { get; set; } = new List<string>();
        [JsonProperty] private List<string> WarriorGearString { get; set; } = new List<string>();
        [JsonProperty] private List<string> MageGearString { get; set; } = new List<string>();

        [JsonProperty] public uint Coins { get; set; }
        [JsonProperty] public uint Upgrades { get; set; }
        [JsonIgnore] public bool IsInitialized { get { return Inv != null; } }

        [JsonIgnore]
        private List<Item> Inv { get; set; } = new List<Item>();

        [JsonProperty]
        public uint MaxInvSize
        {
            get { return BaseInvSize + 10 * Upgrades; }
        }

        [JsonProperty]
        private DateTime lastDailyChest;

        [JsonProperty] public int dailiesInARow = 0;

        [JsonIgnore]
        private List<Item> WarriorGear { get; set; } = new List<Item>();

        [JsonIgnore]
        private List<Item> MageGear { get; set; } = new List<Item>();

        [JsonIgnore]
        public int Count
        { get { return Inv.Count; } }

        [JsonIgnore] public bool IsFull { get { return Count >= MaxInvSize; } }

        [JsonIgnore] public bool HasDuplicate { get { return Inv.Any(i => Inv.Where(j => j.Name.Equals(i.Name)).Count() > 1); } }

        [JsonProperty]
        private Dictionary<ChestQuality, uint> chests = new Dictionary<ChestQuality, uint>()
        {
            { ChestQuality.Wooden, 0 }, {ChestQuality.Normal, 0}, {ChestQuality.Silver, 0}, {ChestQuality.Gold, 0}, {ChestQuality.Adept, 0}, {ChestQuality.Daily, 0}
        };

        public void Initialize()
        {
            Inv = ItemDatabase.GetItems(InvString);
            WarriorGear = new List<Item>();
            foreach (var warriorgear in WarriorGearString.Distinct())
            {
                Equip(warriorgear, ArchType.Warrior);
            }
            MageGear = new List<Item>();
            foreach (var magegear in MageGearString.Distinct())
            {
                Equip(magegear, ArchType.Mage);
            }
            //WarriorGear = ItemDatabase.GetItems(WarriorGearString.Distinct());
            //MageGear = ItemDatabase.GetItems(MageGearString.Distinct());
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context)
        {
            Initialize();
        }

        public int NumberOfItemType(ItemType type)
        {
            return Inv.Where(i => i.ItemType == type).Count();
        }

        public List<string> UnequipExclusiveTo(Element element)
        {
            List<string> removed = new List<string>();
            foreach (var item in WarriorGear)
            {
                if (item.ExclusiveTo != null && item.ExclusiveTo.Contains(element))
                {
                    removed.Add(item.Name);
                }
            }
            foreach (var item in removed)
            {
                Unequip(item);
            }
            return removed;
        }

        public List<Item> GetGear(ArchType arch)
        {
            return (arch == ArchType.Warrior ? WarriorGear : MageGear);
        }

        public bool OpenChest(ChestQuality chestQuality)
        {
            CheckDaily();
            chests.TryGetValue(chestQuality, out uint nOfChests);

            if (nOfChests == 0)
            {
                return false;
            }

            chests.Remove(chestQuality);
            chests.Add(chestQuality, nOfChests - 1);
            return true;
        }

        private void CheckDaily()
        {
            if (lastDailyChest.Date < DateTime.Now.Date && chests[ChestQuality.Daily] == 0)
            {
                if ((DateTime.Now.Date - lastDailyChest.Date).TotalDays <= 1)
                {
                    dailiesInARow++;
                }
                else
                {
                    dailiesInARow = 0;
                }
                AwardChest(ChestQuality.Daily);
                lastDailyChest = DateTime.Now;
            }
        }

        public void AwardChest(ChestQuality chestQuality)
        {
            chests.TryGetValue(chestQuality, out uint nOfChests);

            chests.Remove(chestQuality);
            chests.Add(chestQuality, ++nOfChests);
            UserAccounts.SaveAccounts();
        }

        public string GetChestsToString()
        {
            CheckDaily();
            List<string> s = new List<string>();
            foreach (var cq in chestQualities)
            {
                if (chests[cq] > 0)
                {
                    s.Add($"{ChestIcons[cq]}: {chests[cq]}");
                }
            }
            return string.Join(" - ", s);
        }

        internal bool Remove(string item)
        {
            if (!HasItem(item))
            {
                return false;
            }
            var it = Inv.Where(i => i.Name.Equals(item, StringComparison.InvariantCultureIgnoreCase)).Last();
            if (WarriorGear.Any(i => i.Name.Equals(it.Name, StringComparison.CurrentCultureIgnoreCase)) ||
            MageGear.Any(i => i.Name.Equals(it.Name, StringComparison.CurrentCultureIgnoreCase)))
            {
                if (Inv.Where(i => string.Equals(i.Name, it.Name, StringComparison.InvariantCultureIgnoreCase)).Count() == 1)
                {
                    return false;
                }
            }

            Inv.Remove(it);
            UserAccounts.SaveAccounts();
            return true;
        }

        public string InventoryToString(Detail detail = Detail.none)
        {
            if (Inv.Count == 0)
            {
                return "empty";
            }
            return detail switch
            {
                (Detail.Names) => string.Join(", ", Inv.Select(i => $"{i.IconDisplay} {i.Name}{(i.IsBroken ? " (Broken)" : "")}").ToArray()),
                (Detail.NameAndPrice) => string.Join("\n", Inv.Select(i => $"{i.IconDisplay} {i.Name} - {(Count <= 60 ? "<:coin:569836987767324672>" : "")}{i.Price}").ToArray()),
                _ => string.Join("", Inv.Select(i => i.IconDisplay).ToArray()),
            };
        }

        internal bool HasChest(ChestQuality cq)
        {
            CheckDaily();
            return chests[cq] > 0;
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
            chests = new Dictionary<ChestQuality, uint>()
        {
            { ChestQuality.Wooden, 0 }, {ChestQuality.Normal, 0}, {ChestQuality.Silver, 0}, {ChestQuality.Gold, 0}, {ChestQuality.Adept, 0}, {ChestQuality.Daily, 0}
        };
            Coins = 0;
            Upgrades = 0;
            lastDailyChest = DateTime.Now.Subtract(new TimeSpan(1, 0, 0, 0));
        }

        internal Item GetItem(string item)
        {
            return Inv.Where(s => s.Name.Equals(item, StringComparison.CurrentCultureIgnoreCase) || s.Itemname.Equals(item, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
        }

        public string GearToString(ArchType archType, bool detailed = false)
        {
            var s = new StringBuilder();
            var Gear = GetGear(archType);

            var DefaultIcons = archType == ArchType.Warrior ? WarriorIcons : MageIcons;

            foreach (ItemCategory cat in Item.Equippables)
            {
                s.Append(Gear.GetItem(cat)?.IconDisplay ?? DefaultIcons[cat]);
            }

            return s.ToString();
        }

        internal bool Repair(string item)
        {
            if (!HasItem(item))
            {
                return false;
            }
            var it = GetItem(item);


            if (!RemoveBalance(it.SellValue))
            {
                return false;
            }

            it.IsBroken = false;
            UserAccounts.SaveAccounts();
            return true;
        }

        internal bool Rename(string item, string newname = null)
        {
            if (!HasItem(item)) return false;
            var it = GetItem(item);

            if (!RemoveBalance(it.Price * 2))
            {
                return false;
            }

            it.Nickname = newname ?? "";
            UserAccounts.SaveAccounts();
            return true;
        }

        internal bool Polish(string item)
        {
            if (!HasItem(item)) return false;
            var it = GetItem(item);
            if (!it.CanBeAnimated) return false;
            if (!RemoveBalance(it.Price * 10))
            {
                return false;
            }

            it.IsAnimated = true;
            UserAccounts.SaveAccounts();
            return true;
        }

        public bool HasItem(string item)
        {
            return Inv.Any(s => s.Name.Equals(item, StringComparison.CurrentCultureIgnoreCase) || s.Itemname.Equals(item, StringComparison.CurrentCultureIgnoreCase));
        }

        public bool Equip(string item, ArchType archType)
        {
            if (!HasItem(item))
            {
                return false;
            }

            var i = GetItem(item);
            if (!i.IsEquippable)
            {
                return false;
            }

            var Gear = WarriorGear;

            if (archType == ArchType.Mage)
            {
                if (WarriorExclusive.Contains(i.ItemType))
                {
                    return false;
                }

                Gear = MageGear;
            }
            else
            {
                if (MageExclusive.Contains(i.ItemType))
                {
                    return false;
                }
            }
            Item g = Gear.GetItem(i.Category);
            if (g != null)
            {
                if (g.IsCursed)
                {
                    return false;
                }
                else
                {
                    Gear.Remove(g);
                }
            }

            Gear.Add(i);
            return true;
        }

        [OnSerializing]
        public void OnSerializing(StreamingContext context)
        {
            UpdateStrings();
        }

        private void UpdateStrings()
        {
            InvString.Clear();
            Inv.ForEach(w => InvString?.Add(w.NameToSerialize));

            WarriorGear = WarriorGear.OrderBy(i => i.ItemType).ToList();
            WarriorGearString.Clear();
            WarriorGear.ForEach(w => WarriorGearString.Add(w.Name));

            MageGear = MageGear.OrderBy(i => i.ItemType).ToList();
            MageGearString.Clear();
            MageGear.ForEach(w => MageGearString.Add(w.Name));
        }

        public bool Unequip(string item)
        {
            var it = GetItem(item);
            if (it == null || (!WarriorGear.Any(i => i.Name.Equals(it.Name, StringComparison.CurrentCultureIgnoreCase)) &&
            !MageGear.Any(i => i.Name.Equals(it.Name, StringComparison.CurrentCultureIgnoreCase))))
            {
                return false;
            }

            if (it.IsCursed)
            {
                return false;
            }

            WarriorGear.RemoveAll(i => i.Name.Equals(it.Name, StringComparison.CurrentCultureIgnoreCase));
            MageGear.RemoveAll(i => i.Name.Equals(it.Name, StringComparison.CurrentCultureIgnoreCase));
            UserAccounts.SaveAccounts();
            return true;
        }

        public List<Item> CursedGear()
        {
            return Inv.Where(i => i.IsCursed).ToList();
        }

        public bool RemoveCursedEquipment()
        {
            if (!MageGear.Any(w => w.IsCursed) && !WarriorGear.Any(w => w.IsCursed))
            {
                return false;
            }

            if (!RemoveBalance(10000))
            {
                return false;
            }

            WarriorGear.RemoveAll(i => i.IsCursed);
            MageGear.RemoveAll(i => i.IsCursed);

            return true;
        }

        public bool Add(string item)
        {
            if (IsFull)
            {
                return false;
            }

            var i = ItemDatabase.GetItem(item);
            if (i.Name.Contains("NOT IMPLEMENTED!"))
            {
                return false;
            }

            Inv.Add(i);
            UserAccounts.SaveAccounts();
            return true;
        }

        public bool Buy(string item)
        {
            var i = ItemDatabase.GetItem(item);
            if (i.Name.Contains("NOT IMPLEMENTED!"))
            {
                return false;
            }
            if (IsFull)
            {
                return false;
            }

            if (!RemoveBalance(i.Price))
            {
                return false;
            }

            return Add(i.Name);
        }

        public void Sort()
        {
            Inv = Inv.OrderByDescending(d => WarriorGear.Any(e => e.Name.Equals(d.Name)))
                .ThenByDescending(d => MageGear.Any(e => e.Name.Equals(d.Name)) && !WarriorGear.Any(e => e.Name.Equals(d.Name)))
                .ThenBy(d => d.ItemType)
                .ThenBy(d => d.Name)
                .ToList();
        }

        public bool Sell(string item)
        {
            if (!HasItem(item))
            {
                return false;
            }
            var it = Inv.Where(i => i.Name.Equals(item, StringComparison.InvariantCultureIgnoreCase)).Last();
            if (WarriorGear.Any(i => i.Name.Equals(it.Name, StringComparison.CurrentCultureIgnoreCase)) ||
            MageGear.Any(i => i.Name.Equals(it.Name, StringComparison.CurrentCultureIgnoreCase)))
            {
                if (Inv.Where(i => string.Equals(i.Name, it.Name, StringComparison.InvariantCultureIgnoreCase)).Count() == 1)
                {
                    return false;
                }
            }

            Inv.Remove(it);
            Coins += it.SellValue;
            UserAccounts.SaveAccounts();
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
            if (!HasBalance(amount))
            {
                return false;
            }

            Coins -= amount;
            return true;
        }
    }
}