using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class Inventory
    {
        private List<string> InvString { get; set; } = new List<string>();
        private List<string> WarriorGearString { get; set; } = new List<string>();
        private List<string> MageGearString { get; set; } = new List<string>();
        private uint Coins { get; set; }

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

        public bool Equip(string item)
        {
            throw new NotImplementedException();
        }

        public bool Unequip(string item)
        {
            throw new NotImplementedException();
        }

        public bool Add(string item)
        {
            throw new NotImplementedException();
        }

        public bool Buy(string item)
        {
            throw new NotImplementedException();
        }

        public bool Sell(string item)
        {
            throw new NotImplementedException();
        }

        public void AddBalance(uint amount)
        {
            Coins += amount;
        }

        public bool RemoveBalance(uint amount)
        {
            if (amount > Coins) return false;

            Coins -= amount;
            return true;
        }
    }
}
