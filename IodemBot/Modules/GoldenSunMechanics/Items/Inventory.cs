using IodemBot.Core.UserManagement;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static IodemBot.Modules.GoldenSunMechanics.Psynergy;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public enum ArchType { Warrior, Mage }

    public enum ChestQuality { Wooden, Normal, Silver, Gold, Adept, Daily }

    public class Inventory
    {
        public static readonly int MaxInvSize = 30;
        private static readonly ItemType[] WarriorExclusive = { ItemType.LongSword, ItemType.Shield, ItemType.Helmet, ItemType.HeavyArmor, ItemType.Greave };
        private static readonly ItemType[] MageExclusive = { ItemType.Staff, ItemType.Circlet, ItemType.Bow, ItemType.Robe, ItemType.Bracelet };
        private static readonly ChestQuality[] chestQualities = { ChestQuality.Wooden, ChestQuality.Normal, ChestQuality.Silver, ChestQuality.Gold, ChestQuality.Adept, ChestQuality.Daily };

        public static readonly Dictionary<ChestQuality, string> ChestIcons = new Dictionary<ChestQuality, string>()
        {
            {ChestQuality.Wooden, "<:wooden_chest:570332670576295986>" },
            {ChestQuality.Normal, "<:chest:570332670442078219>" },
            {ChestQuality.Silver, "<:silver_chest:570332670391877678>" },
            {ChestQuality.Gold, "<:gold_chest:570332670530158593>" },
            {ChestQuality.Adept, "<:adept_chest:570332670329094146>" },
            {ChestQuality.Daily, "<:daily_chest:570332670605787157>" }
        };

        [JsonProperty] private List<string> InvString { get; set; }
        [JsonProperty] private List<string> WarriorGearString { get; set; }
        [JsonProperty] private List<string> MageGearString { get; set; }
        [JsonProperty] public uint Coins { get; set; }

        [JsonProperty] private DateTime lastDailyChest;

        [JsonIgnore] private List<Item> Inv;
        [JsonIgnore] private List<Item> WarriorGear;
        [JsonIgnore] private List<Item> MageGear;
        [JsonIgnore] public int Count { get { return Inv.Count; } }
        [JsonIgnore] public bool IsFull { get { return Count >= MaxInvSize; } }
        [JsonIgnore] public bool IsInitialized { get { return Inv != null; } }

        public bool HasDuplicate { get { return Inv.Any(i => Inv.Where(j => j.Name.Equals(i.Name)).Count() > 1); } }

        [JsonProperty]
        private Dictionary<ChestQuality, uint> chests = new Dictionary<ChestQuality, uint>()
        {
            { ChestQuality.Wooden, 0 }, {ChestQuality.Normal, 0}, {ChestQuality.Silver, 0}, {ChestQuality.Gold, 0}, {ChestQuality.Adept, 0}, {ChestQuality.Daily, 0}
        };

        [JsonConstructor]
        public Inventory(List<string> InvString, List<string> WarriorGearString, List<string> MageGearString)
        {
            this.InvString = InvString ?? new List<string>();
            this.WarriorGearString = WarriorGearString ?? new List<string>();
            this.MageGearString = MageGearString ?? new List<string>();

            Inv = ItemDatabase.GetItems(InvString);
            WarriorGear = ItemDatabase.GetItems(WarriorGearString);
            MageGear = ItemDatabase.GetItems(MageGearString);
        }

        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            this.InvString = InvString ?? new List<string>();
            this.WarriorGearString = WarriorGearString ?? new List<string>();
            this.MageGearString = MageGearString ?? new List<string>();

            Inv = ItemDatabase.GetItems(InvString);
            WarriorGear = Inv.Where(i => WarriorGearString.Contains(i.Name)).ToList();
            MageGear = Inv.Where(i => MageGearString.Contains(i.Name)).ToList();
            //WarriorGear = ItemDatabase.GetItems(WarriorGearString);
            //MageGear = ItemDatabase.GetItems(MageGearString);
        }

        public Inventory()
        {
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
            UserAccounts.SaveAccounts();
            return true;
        }

        private void CheckDaily()
        {
            if (lastDailyChest.Date != DateTime.Now.Date && chests[ChestQuality.Daily] == 0)
            {
                AwardChest(ChestQuality.Daily);
            }
            lastDailyChest = DateTime.Now;
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

        public enum Detail { none, Name, PriceAndName }

        public string InventoryToString(Detail detail = Detail.none)
        {
            if (Inv.Count == 0)
            {
                return "empty";
            }
            switch (detail)
            {
                case (Detail.none):
                default:
                    return string.Join("", Inv.Select(i => i.IconDisplay).ToArray());

                case (Detail.Name):
                    return string.Join(", ", Inv.Select(i => $"{i.IconDisplay} {i.Name}{(i.IsBroken ? " (Broken)" : "")}").ToArray());

                case (Detail.PriceAndName):
                    return string.Join("\n", Inv.Select(i => $"{i.IconDisplay} {i.Name} - <:coin:569836987767324672>{i.Price}").ToArray());
            }
        }

        public void Clear()
        {
            Inv.Clear();
            WarriorGear.Clear();
            MageGear.Clear();
            UpdateStrings();
        }

        public string GearToString(ArchType archType, bool detailed = false)
        {
            var s = new StringBuilder();
            var Gear = WarriorGear;
            if (archType == ArchType.Mage)
            {
                Gear = MageGear;
            }

            var weapon = Gear.Where(i => i.IsWeapon).FirstOrDefault();
            s.Append(weapon != null ? weapon.IconDisplay : (archType == ArchType.Warrior ? "<:Swords:572526110357585920>" : "<:Staves:572526110370168851>"));

            var armwear = Gear.Where(i => i.IsArmWear).FirstOrDefault();
            s.Append(armwear != null ? armwear.IconDisplay : (archType == ArchType.Warrior ? "<:Shields:572526110118641664>" : "<:Armlets:572526109908795402>"));

            var headwear = Gear.Where(i => i.IsHeadWear).FirstOrDefault();
            s.Append(headwear != null ? headwear.IconDisplay : (archType == ArchType.Warrior ? "<:Helmets:572526110055858226>" : "<:Circlets:572526110101864448>"));

            var chestwear = Gear.Where(i => i.IsChestWear).FirstOrDefault();
            s.Append(chestwear != null ? chestwear.IconDisplay : (archType == ArchType.Warrior ? "<:Armors:572526109942611978>" : "<:Robes:572526110068441118>"));

            var underwear = Gear.Where(i => i.IsUnderWear).FirstOrDefault();
            s.Append(underwear != null ? underwear.IconDisplay : "<:Shirts:572526110173167616>");

            var boots = Gear.Where(i => i.IsFootWear).FirstOrDefault();
            s.Append(boots != null ? boots.IconDisplay : "<:Boots:572526109975904257>");

            var ring = Gear.Where(i => i.IsAccessoire).FirstOrDefault();
            s.Append(ring != null ? ring.IconDisplay : "<:Rings:572526110060052482>");

            return s.ToString();
        }

        internal bool Repair(string item)
        {
            var it = Inv.Where(i => i.Name.Equals(item, StringComparison.CurrentCultureIgnoreCase) && i.IsBroken).FirstOrDefault();
            if (it == null)
            {
                return false;
            }

            if (!RemoveBalance(it.SellValue))
            {
                return false;
            }

            it.IsBroken = false;
            UpdateStrings();
            return true;
        }

        public bool HasItem(string item)
        {
            return Inv.Any(s => string.Equals(s.Name, item, StringComparison.CurrentCultureIgnoreCase));
        }

        public bool Equip(string item, ArchType archType)
        {
            if (!HasItem(item))
            {
                return false;
            }

            var i = ItemDatabase.GetItem(item);
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
            Item g;
            if (i.IsWeapon)
            {
                g = Gear.Where(w => w.IsWeapon).FirstOrDefault();
                if (g != null && g.IsCursed)
                {
                    return false;
                }

                Gear.RemoveAll(w => w.IsWeapon);
            }

            if (i.IsHeadWear)
            {
                g = Gear.Where(w => w.IsHeadWear).FirstOrDefault();
                if (g != null && g.IsCursed)
                {
                    return false;
                }

                Gear.RemoveAll(w => w.IsHeadWear);
            }

            if (i.IsChestWear)
            {
                g = Gear.Where(w => w.IsChestWear).FirstOrDefault();
                if (g != null && g.IsCursed)
                {
                    return false;
                }

                Gear.RemoveAll(w => w.IsChestWear);
            }

            if (i.IsArmWear)
            {
                g = Gear.Where(w => w.IsArmWear).FirstOrDefault();
                if (g != null && g.IsCursed)
                {
                    return false;
                }

                Gear.RemoveAll(w => w.IsArmWear);
            }

            if (i.IsUnderWear)
            {
                g = Gear.Where(w => w.IsUnderWear).FirstOrDefault();
                if (g != null && g.IsCursed)
                {
                    return false;
                }

                Gear.RemoveAll(w => w.IsUnderWear);
            }

            if (i.IsFootWear)
            {
                g = Gear.Where(w => w.IsFootWear).FirstOrDefault();
                if (g != null && g.IsCursed)
                {
                    return false;
                }

                Gear.RemoveAll(w => w.IsFootWear);
            }

            if (i.IsAccessoire)
            {
                g = Gear.Where(w => w.IsAccessoire).FirstOrDefault();
                if (g != null && g.IsCursed)
                {
                    return false;
                }

                Gear.RemoveAll(w => w.IsAccessoire);
            }

            Gear.Add(i);
            UpdateStrings();
            return true;
        }

        private void UpdateStrings()
        {
            InvString.Clear();
            Inv.ForEach(w => InvString.Add(w.NameAndBroken));

            WarriorGear = WarriorGear.OrderBy(i => i.ItemType).ToList();
            WarriorGearString.Clear();
            WarriorGear.ForEach(w => WarriorGearString.Add(w.Name));

            MageGear = MageGear.OrderBy(i => i.ItemType).ToList();
            MageGearString.Clear();
            MageGear.ForEach(w => MageGearString.Add(w.Name));

            UserAccounts.SaveAccounts();
        }

        public bool Unequip(string item)
        {
            var it = ItemDatabase.GetItem(item);
            if (!WarriorGear.Any(i => i.Name.Equals(it.Name, StringComparison.CurrentCultureIgnoreCase)) &&
            !MageGear.Any(i => i.Name.Equals(it.Name, StringComparison.CurrentCultureIgnoreCase)))
            {
                return false;
            }

            if (it.IsCursed)
            {
                return false;
            }

            WarriorGear.RemoveAll(i => i.Name.Equals(it.Name, StringComparison.CurrentCultureIgnoreCase));
            MageGear.RemoveAll(i => i.Name.Equals(it.Name, StringComparison.CurrentCultureIgnoreCase));

            UpdateStrings();

            return true;
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

            UpdateStrings();
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
            UpdateStrings();
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
            UpdateStrings();
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
            UpdateStrings();
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