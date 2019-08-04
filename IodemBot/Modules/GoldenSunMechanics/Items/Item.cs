using Discord;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using static IodemBot.Modules.GoldenSunMechanics.Psynergy;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public enum ItemType
    {
        Collectible,
        LongSword, Axe, Staff, LightBlade, Mace, Bow, Claw,
        Shield, Bracelet, Glove,
        Helmet, Hat, Circlet, Crown,
        HeavyArmor, Robe, LightArmor,
        UnderWear,
        Boots, Greave,
        Ring, Misc
    }

    public enum ItemCategory
    {
        Weapon, ArmWear, ChestWear, HeadWear, UnderWear, FootWear, Accessoire, Other
    }

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
            {ItemCategory.Other, new [] {ItemType.Collectible } }
        };

        public static readonly ItemCategory[] Equippables = new[] { ItemCategory.Weapon, ItemCategory.ArmWear, ItemCategory.ChestWear, ItemCategory.HeadWear, ItemCategory.UnderWear, ItemCategory.FootWear, ItemCategory.Accessoire };

        public string Name { get; set; }
        internal string NameAndBroken { get { return $"{Name}{(IsBroken ? "(B)" : "")}"; } }
        public string IconDisplay { get { return $"{(IsBroken ? "(" : "")}{Icon}{(IsBroken ? ")" : "")}"; } }
        public string Icon { get; set; }
        public uint Price { get; set; }

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

        [JsonConverter(typeof(StringEnumConverter))]
        public ItemType ItemType { get; set; }

        public ItemCategory Category
        {
            get { return ItemCategorization.Where(k => k.Value.Contains(ItemType)).First().Key; }
        }

        public Element[] ExclusiveTo { get; set; }
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
                s.Append("\n");
            }
            if (MultStatsOnEquip.MultipliersToString() != "``")
            {
                s.Append(MultStatsOnEquip.MultipliersToString());
                s.Append("\n");
            }
            if (AddElStatsOnEquip.NonZerosToString() != "")
            {
                s.Append(AddElStatsOnEquip.NonZerosToString());
                s.Append("\n");
            }

            var various = new List<string>();
            if (HPRegen > 0)
            {
                various.Add($"HP Regen: {HPRegen}");
            }

            if (DamageAlignment != Element.none)
            {
                various.Add($"{GoldenSun.ElementIcons[DamageAlignment]}");
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
                various.Add($"{(Category == ItemCategory.Weapon ? "" : $"{(GrantsUnleash ? "Adds an Effect to your Artifacts Unleash: " : $"{(ChanceToActivate < 100 ? "May target" : "Targets")} the Wearer with an Effect: ")}")}{Unleash.ToString()}");
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
            s.Append(string.Join(" | ", various));
            return s.ToString();
        }
    }

    public class Unleash
    {
        public string UnleashName { get; set; }

        [DefaultValue(Element.none)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public Element UnleashAlignment { get; set; }

        [JsonIgnore] internal List<IEffect> DefaultEffects { get; set; }
        [JsonProperty] internal List<EffectImage> EffectImages { get; set; }
        [JsonIgnore] internal List<IEffect> AdditionalEffects { get; set; } = new List<IEffect>();

        [JsonIgnore]
        public List<IEffect> Effects
        {
            get
            {
                var eff = new List<IEffect>();
                eff.AddRange(DefaultEffects);
                eff.AddRange(AdditionalEffects);
                return eff;
            }
        }

        public Unleash(List<EffectImage> effectImages)
        {
            DefaultEffects = new List<IEffect>();
            EffectImages = effectImages;
            if (effectImages != null)
            {
                effectImages.ForEach(e => DefaultEffects.Add(IEffect.EffectFactory(e.Id, e.Args)));
            }
        }

        public override string ToString()
        {
            var s = new StringBuilder();
            if (UnleashName != null)
            {
                s.Append(UnleashName);
            }

            if (Effects.Count > 0)
            {
                if (UnleashName != null)
                {
                    s.Append(" (");
                }

                if (UnleashAlignment != Element.none)
                {
                    s.Append(GoldenSun.ElementIcons[UnleashAlignment]);
                }
                s.Append(string.Join(", ", Effects.Select(e => $"{e.ToString()}")));
                if (UnleashName != null)
                {
                    s.Append(")");
                }
            }
            return s.ToString();
        }
    }
}