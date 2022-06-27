using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Discord;
using IodemBot.Extensions;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class Item : ICloneable
    {
        private static readonly Dictionary<ItemCategory, ItemType[]> ItemCategorization = new()
        {
            {
                ItemCategory.Weapon,
                new[]
                {
                    ItemType.LongSword, ItemType.Axe, ItemType.Staff, ItemType.LightBlade, ItemType.Mace, ItemType.Bow,
                    ItemType.Claw
                }
            },
            { ItemCategory.ArmWear, new[] { ItemType.Shield, ItemType.Bracelet, ItemType.Glove } },
            { ItemCategory.ChestWear, new[] { ItemType.HeavyArmor, ItemType.Robe, ItemType.LightArmor } },
            { ItemCategory.HeadWear, new[] { ItemType.Helmet, ItemType.Hat, ItemType.Circlet, ItemType.Crown } },
            { ItemCategory.UnderWear, new[] { ItemType.UnderWear } },
            { ItemCategory.FootWear, new[] { ItemType.Boots, ItemType.Greave } },
            { ItemCategory.Accessory, new[] { ItemType.Ring, ItemType.Misc } },
            { ItemCategory.Other, new[] { ItemType.Collectible } }
        };

        public static readonly ItemCategory[] Equippables =
        {
            ItemCategory.Weapon, ItemCategory.ArmWear, ItemCategory.ChestWear, ItemCategory.HeadWear,
            ItemCategory.UnderWear, ItemCategory.FootWear, ItemCategory.Accessory
        };

        public static readonly Dictionary<ItemRarity, uint> MinimumTicketPrices = new()
        {
            { ItemRarity.Common, 200 },
            { ItemRarity.Uncommon, 2000 },
            { ItemRarity.Rare, 5000},
            { ItemRarity.Legendary, 12000 },
            { ItemRarity.Mythical, 22000 },
            { ItemRarity.Unique, 1000000 }
        };

        [JsonIgnore] internal bool IsAnimated = false;

        public string Name
        {
            get => Nickname.IsNullOrEmpty() ? Itemname : Nickname;
            set => Itemname = value;
        }

        public string Nickname { get; set; }
        public string Itemname { get; set; }

        internal string NameToSerialize =>
            $"{Itemname}" +
            $"{(IsAnimated ? "(A)" : "")}" +
            $"{(IsBroken ? "(B)" : "")}" +
            $"{(IsBoughtFromShop ? "(S)" : "")}" +
            $"{(!Nickname.IsNullOrEmpty() ? $"|{Nickname}" : "")}";

        public string NormalIcon { get; set; }
        public string AnimatedIcon { get; set; }
        [JsonIgnore] internal bool CanBeAnimated => !IsAnimated && !AnimatedIcon.IsNullOrEmpty();
        public string IconDisplay => IsBroken ? $"({Icon})$" : Icon;

        public string Icon
        {
            get => IsAnimated ? AnimatedIcon : NormalIcon;
            set => NormalIcon = value;
        }

        public uint Price { get; set; }
        [JsonIgnore] public uint SellValue => (uint)(Price / (IsBroken ? 10 : 2));

        public uint TicketPrice
        {
            get
            {
                return Math.Max(1, Math.Max(MinimumTicketPrices[Rarity], Price) / (Inventory.GameTicketValue + 8 * (4 - (uint)Rarity)));
            }
        }

        public uint TicketValue => (uint)Rarity + 1;

        public string Description { get; set; }

        public ItemRarity Rarity { get; set; }

        public Color Color =>
            Category == ItemCategory.Weapon && IsUnleashable ? Colors.Get(Unleash.UnleashAlignment.ToString()) :
            IsArtifact ? Colors.Get("Artifact") : Colors.Get("Exathi");

        public ItemType ItemType { get; set; }

        public ItemCategory Category
        {
            get { return ItemCategorization.First(k => k.Value.Contains(ItemType)).Key; }
        }

        public Element[] ExclusiveTo { get; set; } = Array.Empty<Element>();
        public bool IsStackable { get; set; }

        public bool IsUsable { get; set; } = false;
        public bool IsArtifact { get; set; } = false;

        public bool IsBoughtFromShop { get; set; } = false;
        public int IncreaseUnleashRate { get; set; }

        [DefaultValue(100)] public int ChanceToActivate { get; set; } = 80;

        public int ChanceToBreak { get; set; } = 100;

        public bool IsBroken { get; set; }

        public bool IsUnleashable => Unleash != null;
        public bool GrantsUnleash { get; set; }
        public Unleash Unleash { get; set; }

        [DefaultValue(Element.None)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public Element DamageAlignment { get; set; }

        public bool IsCursed { get; set; }
        public bool CuresCurse { get; set; }

        public bool IsEquippable => Equippables.Contains(Category);

        public bool IsEquipped { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Stats AddStatsOnEquip { get; set; } = new(0, 0, 0, 0, 0);

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Stats MultStatsOnEquip { get; set; } = new(100, 100, 100, 100, 100);

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ElementalStats AddElStatsOnEquip { get; set; } = new(0, 0, 0, 0, 0, 0, 0, 0);

        public int HpRegen { get; set; }
        public int PpRegen { get; set; }

        public object Clone()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<Item>(serialized);
        }

        public override string ToString()
        {
            return Name;
        }

        public bool IsEquippableBy(ArchType archType)
        {
            if (archType == ArchType.Mage)
                return !Inventory.WarriorExclusive.Contains(ItemType);
            else
                return !Inventory.MageExclusive.Contains(ItemType);
        }

        public string Summary()
        {
            var s = new StringBuilder();

            if (AddStatsOnEquip.NonZerosToString() != "``")
            {
                s.Append(AddStatsOnEquip.NonZerosToString());
                s.Append('\n');
            }

            if (MultStatsOnEquip.MultipliersToString() != "``")
            {
                s.Append(MultStatsOnEquip.MultipliersToString());
                s.Append('\n');
            }

            if (AddElStatsOnEquip.NonZerosToString() != "")
            {
                s.Append(AddElStatsOnEquip.NonZerosToString());
                s.Append('\n');
            }

            var various = new List<string>();
            if (HpRegen > 0) various.Add($"HP Regen: {HpRegen}");

            if (DamageAlignment != Element.None) various.Add($"{Emotes.GetIcon(DamageAlignment)}");

            if (PpRegen > 0) various.Add($"PP Regen: {PpRegen}");

            if (IncreaseUnleashRate > 0) various.Add($"Increases unleash Rate by {IncreaseUnleashRate}%");

            if (IsUnleashable)
                various.Add(
                    $"{(Category == ItemCategory.Weapon ? "" : $"{(GrantsUnleash ? "Adds an Effect to your Artifacts Unleash: " : $"{(ChanceToActivate < 100 ? $"{ChanceToActivate}% chance to target" : "Targets")} the wearer with an Effect: ")}")}{Unleash}{(!GrantsUnleash && ChanceToBreak > 0 && Category != ItemCategory.Weapon ? $"\n{ChanceToBreak}% chance to break on activation." : "")}");

            if (CuresCurse) various.Add("Cures Curse");

            if (IsCursed) various.Add("Cursed");

            if (Name == "Lure Cap")
                various.Add(
                    "This cap illuminates the area and will make you and your team find chests more easily. But watch out, it might attract more enemies!");

            if (CanBeAnimated) various.Add("Polishable");
            s.Append(string.Join(" | ", various));

            if (!Description.IsNullOrEmpty())
            {
                if (various.Count > 0) s.Append('\n');
                s.Append('\n');
                s.Append($"*{Description}*");
            }

            return s.ToString();
        }
    }

    public class Unleash
    {
        public string UnleashName { get; set; }

        [DefaultValue(Element.None)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public Element UnleashAlignment { get; set; }

        [JsonProperty] public List<Effect> Effects { get; set; } = new();

        [JsonIgnore] internal List<Effect> AdditionalEffects { get; set; } = new();

        [JsonIgnore]
        public List<Effect> AllEffects
        {
            get
            {
                var eff = new List<Effect>();
                eff.AddRange(Effects);
                eff.AddRange(AdditionalEffects);
                return eff;
            }
        }

        public override string ToString()
        {
            var s = new StringBuilder();
            if (UnleashName != null) s.Append(UnleashName);

            if (AllEffects.Count > 0)
            {
                if (UnleashName != null) s.Append(" (");

                if (UnleashAlignment != Element.None) s.Append(Emotes.GetIcon(UnleashAlignment));
                s.Append(string.Join(", ", AllEffects.Select(e => $"{e}")));
                if (UnleashName != null) s.Append(')');
            }

            return s.ToString();
        }
    }
}