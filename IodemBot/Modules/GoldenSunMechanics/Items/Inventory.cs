using IodemBot.Core.UserManagement;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public enum ArchType { Warrior, Mage }

    public class Inventory
    {
        private static int inventorySize = 24;
        private static ItemType[] WarriorExclusive = {ItemType.LongSword, ItemType.Shield, ItemType.Helmet, ItemType.HeavyArmor, ItemType.HeavyBoots };
        private static ItemType[] MageExclusive = { ItemType.Stave, ItemType.Circlet, ItemType.Bow, ItemType.Robe, ItemType.Armlet };

        [JsonProperty] private List<string> InvString { get; set; } = new List<string>();
        [JsonProperty] private List<string> WarriorGearString { get; set; } = new List<string>();
        [JsonProperty] private List<string> MageGearString { get; set; } = new List<string>();
        [JsonProperty] public uint Coins { get; set; }

        [JsonIgnore]
        private List<Item> Inv = new List<Item>();

        [JsonIgnore]
        private List<Item> WarriorGear = new List<Item>();

        [JsonIgnore]
        private List<Item> MageGear = new List<Item>();

        public Inventory(List<string> InvString, List<string> WarriorGearString, List<string> MageGearString)
        {
            this.InvString = InvString;
            this.WarriorGearString = WarriorGearString;
            this.MageGearString = MageGearString;

            Inv = ItemDatabase.GetItems(InvString);
            WarriorGear = ItemDatabase.GetItems(WarriorGearString);
            MageGear = ItemDatabase.GetItems(MageGearString);
        }

        public List<Item> GetGear(ArchType arch)
        {
            return (arch == ArchType.Warrior ? WarriorGear : MageGear);
        }

        public string InventoryToString(bool detailed = false)
        {
            if (Inv.Count == 0)
            {
                return "empty";
            }

            return string.Join("", Inv.Select(i => i.Icon).ToArray());
        }

        public string GearToString(ArchType archType, bool detailed = false)
        {
            var s = new StringBuilder();
            var Gear = WarriorGear;
            if (archType == ArchType.Mage)
            {
                Gear = MageGear;
            }

            var weapon = Gear.Where(i => i.IsWeapon()).FirstOrDefault();
            s.Append(weapon != null ? weapon.Icon : (archType == ArchType.Warrior ? "<:SwordBW:569890243755638795>" : "<:StaveBW:569890244133126160>"));

            var armwear = Gear.Where(i => i.IsArmWear()).FirstOrDefault();
            s.Append(armwear != null ? armwear.Icon : (archType == ArchType.Warrior ? "<:ShieldBW:569890244120281103>" : "<:ArmletBW:569890244057497610>"));

            var headwear = Gear.Where(i => i.IsHeadWear()).FirstOrDefault();
            s.Append(headwear != null ? headwear.Icon : (archType == ArchType.Warrior ? "<:HelmetBW:569890244175069194>" : "<:CircletBW:569890244053434368>"));

            var chestwear = Gear.Where(i => i.IsChestWear()).FirstOrDefault();
            s.Append(chestwear != null ? chestwear.Icon : (archType == ArchType.Warrior ? "<:ArmorBW:569890244074274846>" : "<:RobeBW:569890243629547573>"));

            var underwear = Gear.Where(i => i.ItemType == ItemType.UnderWear).FirstOrDefault();
            s.Append(underwear != null ? underwear.Icon : "<:UndershirtBW:569890244154097697>");

            var boots = Gear.Where(i => i.ItemType == ItemType.Boots).FirstOrDefault();
            s.Append(boots != null ? boots.Icon : "<:BootsBW:569890244082663436>");

            var ring = Gear.Where(i => i.ItemType == ItemType.Ring).FirstOrDefault();
            s.Append(ring != null ? ring.Icon : "<:RingBW:569890244141252608>");

            return s.ToString();
        }

        public bool HasItem(string item)
        {
            return Inv.Any(s => s.Name == item);
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
            var GearString = WarriorGearString;

            if (archType == ArchType.Mage)
            {
                if (WarriorExclusive.Contains(i.ItemType)) return false;
                Gear = MageGear;
                GearString = MageGearString;
            } else
            {
                if (MageExclusive.Contains(i.ItemType)) return false;
            }

            if (i.IsWeapon())
            {
                Gear.RemoveAll(w => w.IsWeapon());
            }

            if (i.IsHeadWear())
            {
                Gear.RemoveAll(w => w.IsHeadWear());
            }

            if (i.IsChestWear())
            {
                Gear.RemoveAll(w => w.IsChestWear());
            }

            if (i.IsArmWear())
            {
                Gear.RemoveAll(w => w.IsArmWear());
            }

            if (i.ItemType == ItemType.Boots)
            {
                Gear.RemoveAll(w => w.ItemType == ItemType.Boots);
            }

            if (i.ItemType == ItemType.Ring)
            {
                Gear.RemoveAll(w => w.ItemType == ItemType.Ring);
            }

            if (i.ItemType == ItemType.UnderWear)
            {
                Gear.RemoveAll(w => w.ItemType == ItemType.UnderWear);
            }

            Gear.Add(i);
            UpdateStrings();
            return true;
        }

        private void UpdateStrings()
        {
            InvString.Clear();
            Inv.ForEach(w => InvString.Add(w.Name));

            WarriorGearString.Clear();
            WarriorGear.ForEach(w => WarriorGearString.Add(w.Name));

            MageGearString.Clear();
            MageGear.ForEach(w => MageGearString.Add(w.Name));

            UserAccounts.SaveAccounts();
        }

        public bool Unequip(string item)
        {
            var it = ItemDatabase.GetItem(item);
            if (!WarriorGear.Any(i => i.Name.Equals(it.Name, StringComparison.CurrentCultureIgnoreCase)) &&
            !MageGear.Any(i => i.Name.Equals(it.Name, StringComparison.CurrentCultureIgnoreCase))) return false;

            if (it.IsCursed) return false;

            WarriorGear.RemoveAll(i => i.Name.Equals(it.Name, StringComparison.CurrentCultureIgnoreCase));
            MageGear.RemoveAll(i => i.Name.Equals(it.Name, StringComparison.CurrentCultureIgnoreCase));

            UpdateStrings();

            return true;
        }

        public bool RemoveCursedEquipment()
        {
            if (!RemoveBalance(10000)) return false;

            WarriorGear.RemoveAll(i => i.IsCursed);
            MageGear.RemoveAll(i => i.IsCursed);

            UpdateStrings();
            return true;
        }

        public bool Add(string item)
        {
            var i = ItemDatabase.GetItem(item);
            if (i.Name.Contains("NOT IMPLEMENTED!"))
            {
                return false;
            }

            if (HasItem(item))
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

            if (HasItem(item))
            {
                return false;
            }

            if (!RemoveBalance(i.Price))
                return false;
            return Add(i.Name);
            
        }

        public void Sort()
        {
            Inv = Inv.OrderBy(d => WarriorGear.Contains(d)).ThenBy(d => MageGear.Contains(d)).ThenBy(d => d.ItemType).ThenBy(d => d.Name).ToList();
            UpdateStrings();
        }

        public bool Sell(string item)
        {
            var it = Inv.Where(i => i.Name == item).FirstOrDefault();
            if (it == null) return false;
            if (it.IsEquipped) return false;
            if (WarriorGear.Any(i => i.Name.Equals(it.Name, StringComparison.CurrentCultureIgnoreCase)) ||
            MageGear.Any(i => i.Name.Equals(it.Name, StringComparison.CurrentCultureIgnoreCase))) return false;

            Inv.Remove(it);
            Coins += it.sellValue;
            UpdateStrings();
            return true;
        }

        public void AddBalance(uint amount)
        {
            Coins += amount;
        }

        public bool RemoveBalance(uint amount)
        {
            if (amount > Coins)
            {
                return false;
            }

            Coins -= amount;
            return true;
        }
    }
}