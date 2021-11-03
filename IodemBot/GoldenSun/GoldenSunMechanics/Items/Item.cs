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
        private static readonly Dictionary<ItemCategory, ItemType[]> ItemCategorization = new Dictionary<ItemCategory, ItemType[]>()
        {
            { ItemCategory.Weapon, new [] { ItemType.LongSword, ItemType.Axe, ItemType.Staff, ItemType.LightBlade, ItemType.Mace, ItemType.Bow, ItemType.Claw } },
            { ItemCategory.ArmWear, new [] { ItemType.Shield, ItemType.Bracelet, ItemType.Glove } },
            { ItemCategory.ChestWear, new []  { ItemType.HeavyArmor, ItemType.Robe, ItemType.LightArmor } },
            { ItemCategory.HeadWear, new []{ ItemType.Helmet, ItemType.Hat, ItemType.Circlet, ItemType.Crown } },
            { ItemCategory.UnderWear, new[] { ItemType.UnderWear} },
            { ItemCategory.FootWear, new [] { ItemType.Boots, ItemType.Greave } },
            { ItemCategory.Accessoire, new []{ ItemType.Ring, ItemType.Misc } },
            { ItemCategory.Other, new [] {ItemType.Collectible } }
        };

        public static readonly ItemCategory[] Equippables = new[] { ItemCategory.Weapon, ItemCategory.ArmWear, ItemCategory.ChestWear, ItemCategory.HeadWear, ItemCategory.UnderWear, ItemCategory.FootWear, ItemCategory.Accessoire };

        public string Name { get { return Nickname.IsNullOrEmpty() ? Itemname : Nickname; } set { Itemname = value; } }

        public string Nickname { get; set; }
        public string Itemname { get; set; }

        internal string NameToSerialize { get { return $"{Itemname}{(IsAnimated ? "(A)" : "")}{(IsBroken ? "(B)" : "")}{(!Nickname.IsNullOrEmpty() ? $"|{Nickname}" : "")}"; } }
        public string IconDisplay { get { return $"{(IsBroken ? "(" : "")}{Icon}{(IsBroken ? ")" : "")}"; } }
        public string Icon { get { return IsAnimated ? AnimatedIcon : NormalIcon; } set { NormalIcon = value; } }

        public string NormalIcon { get; set; }
        public string AnimatedIcon { get; set; }
        [JsonIgnore] internal bool IsAnimated = false;
        [JsonIgnore] internal bool CanBeAnimated { get { return !IsAnimated && !AnimatedIcon.IsNullOrEmpty(); } }

        public string Sprite { get { return IsAnimated ? AnimatedIcon : Icon; } }

        public uint Price { get; set; }

        public string Description { get;set; }

        public ItemRarity Rarity { get; set; }

        public Color Color
        {
            get
            {
                return (Category == ItemCategory.Weapon && IsUnleashable) ? Colors.Get(Unleash.UnleashAlignment.ToString()) :
                     IsArtifact ? Colors.Get("Artifact") : Colors.Get("Exathi");
            }
        }

        [JsonIgnore]
        public uint SellValue { get { return (uint)(Price / (IsBroken ? 10 : 2)); } }

        public ItemType ItemType { get; set; }

        public ItemCategory Category
        {
            get { return ItemCategorization.First(k => k.Value.Contains(ItemType)).Key; }
        }

        public Element[] ExclusiveTo { get; set; } = Array.Empty<Element>();
        public bool IsStackable { get; set; }

        public bool IsUsable { get; set; } = false;
        public bool IsArtifact { get; set; } = false;

        public int IncreaseUnleashRate { get; set; }

        [DefaultValue(100)]
        public int ChanceToActivate { get; set; } = 80;

        public int ChanceToBreak { get; set; } = 100;

        public bool IsBroken { get; set; }

        public bool IsUnleashable { get { return Unleash != null; } }
        public bool GrantsUnleash { get; set; }
        public Unleash Unleash { get; set; }

        [DefaultValue(Element.none)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public Element DamageAlignment { get; set; }

        public bool IsCursed { get; set; }
        public bool CuresCurse { get; set; }

        public bool IsEquippable { get { return Equippables.Contains(Category); } }
        public bool IsEquipped { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Stats AddStatsOnEquip { get; set; } = new Stats(0, 0, 0, 0, 0);

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Stats MultStatsOnEquip { get; set; } = new Stats(100, 100, 100, 100, 100);

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ElementalStats AddElStatsOnEquip { get; set; } = new ElementalStats(0, 0, 0, 0, 0, 0, 0, 0);

        public int HPRegen { get; set; }
        public int PPRegen { get; set; }

        public object Clone()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<Item>(serialized);
        }

        public override string ToString()
        {
            return Name;
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
            if (HPRegen > 0)
            {
                various.Add($"HP Regen: {HPRegen}");
            }

            if (DamageAlignment != Element.none)
            {
                various.Add($"{Emotes.GetIcon(DamageAlignment)}");
            }

            if (PPRegen > 0)
            {
                various.Add($"PP Regen: {PPRegen}");
            }

            if (IncreaseUnleashRate > 0)
            {
                various.Add($"Increases unleash Rate by {IncreaseUnleashRate}%");
            }

            if (IsUnleashable)
            {
                various.Add($"{(Category == ItemCategory.Weapon ? "" : $"{(GrantsUnleash ? "Adds an Effect to your Artifacts Unleash: " : $"{(ChanceToActivate < 100 ? $"{ChanceToActivate}% chance to target" : "Targets")} the wearer with an Effect: ")}")}{Unleash}{(!GrantsUnleash && ChanceToBreak > 0 && Category != ItemCategory.Weapon ? $"\n{ChanceToBreak}% chance to break on activation." : "")}");
            }

            if (CuresCurse)
            {
                various.Add($"Cures Curse");
            }

            if (IsCursed)
            {
                various.Add($"Cursed");
            }

            if (Name == "Lure Cap")
            {
                various.Add("This cap illuminates the area and will make you and your team find chests more easily. But watch out, it might attract more enemies!");
            }

            if (CanBeAnimated)
            {
                various.Add($"Polishable");
            }
            s.Append(string.Join(" | ", various));

            if (!Description.IsNullOrEmpty())
            {   
                if(various.Count > 0)
                {
                    s.Append('\n');
                }
                s.Append('\n'); s.Append($"*{Description}*");
            }
            return s.ToString();
        }
    }

    public class Unleash
    {
        public string UnleashName { get; set; }

        [DefaultValue(Element.none)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public Element UnleashAlignment { get; set; }

        [JsonProperty]
        public List<Effect> Effects { get; set; } = new List<Effect>();

        [JsonIgnore] internal List<Effect> AdditionalEffects { get; set; } = new List<Effect>();

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
            if (UnleashName != null)
            {
                s.Append(UnleashName);
            }

            if (AllEffects.Count > 0)
            {
                if (UnleashName != null)
                {
                    s.Append(" (");
                }

                if (UnleashAlignment != Element.none)
                {
                    s.Append(Emotes.GetIcon(UnleashAlignment));
                }
                s.Append(string.Join(", ", AllEffects.Select(e => $"{e}")));
                if (UnleashName != null)
                {
                    s.Append(')');
                }
            }
            return s.ToString();
        }
    }
}