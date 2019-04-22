using Discord;
using IodemBot.Core.UserManagement;
using IodemBot.Modules.ColossoBattles;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IodemBot.Modules.GoldenSunMechanics.Psynergy;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public enum ItemType { Collectible,
        LongSword, Axe, Staff, LightBlade, Mace, Bow, Claw,
        Shield, Armlet, Glove,
        HeavyArmor, Robe, LightArmor,
        HardHat, LightHat, Circlet, Crown,
        UnderWear, Boot, Ring
    }
    public class Item : ICloneable
    {
        private static ItemType[] Weapons = { ItemType.LongSword, ItemType.Axe, ItemType.Staff, ItemType.LightBlade, ItemType.Mace, ItemType.Bow, ItemType.Claw };
        private static ItemType[] ArmWear = { ItemType.Shield, ItemType.Armlet, ItemType.Glove };
        private static ItemType[] ChestWear = { ItemType.HeavyArmor, ItemType.Robe, ItemType.LightArmor };
        private static ItemType[] HeadWear = { ItemType.HardHat, ItemType.LightHat, ItemType.Circlet, ItemType.Crown };

        public string Name { get; set; }
        public IEmote Icon { get; set; }
        public uint Price { get; set; }

        [JsonIgnore]
        public uint sellValue { get { return Price / 2; } }

        [JsonConverter(typeof(StringEnumConverter))]
        public ItemType ItemType { get; set; }

        public Element[] ExclusiveTo { get; set; }
        public bool IsStackable { get; set; }

        public bool IsUsable { get; set; } = false;
        public bool IsArtifact { get; set; } = false;

        public bool IsUnleashable { get; set; }
        public Unleash unleash { get; set; }

        [DefaultValue(Element.none)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public Element DamageAlignment { get; set; }

        public bool IsCursed { get; set; }
        public bool CuresCurse { get; set; }

        public bool IsEquippable { get; set; }
        public bool IsEquipped { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Stats AddStatsOnEquip { get; set; } = new Stats(0, 0, 0, 0, 0);

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Stats MultStatsOnEquip { get; set; } = new Stats(1, 1, 1, 1, 1);

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ElementalStats AddElStatsOnEquip { get; set; } = new ElementalStats(0, 0, 0, 0, 0, 0, 0, 0);

        public int HPRegen { get; set; }
        public int PPRegen { get; set; }

        public bool IsWeapon()
        {
            return Weapons.Contains(ItemType);
        }

        public bool IsHeadWear()
        {
            return HeadWear.Contains(ItemType);
        }

        public bool IsChestWear()
        {
            return ChestWear.Contains(ItemType);
        }

        public bool IsArmWear()
        {
            return ArmWear.Contains(ItemType);
        }

        public object Clone()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<Item>(serialized);
        }
    }

    public class Unleash {
        public string UnleashName { get; set; }

        [DefaultValue(Element.none)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public Element UnleashAlignment { get; set; }

        [DefaultValue(35)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int UnleashRate { get; set; }

        public IEffect EffectOnUnleash { get; set; }
    }
}
