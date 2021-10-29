using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace IodemBot
{
    public enum DjinnDetail { None, Names }

    public enum Team { A, B }

    public enum Condition { Down, Poison, Venom, Seal, Stun, DeathCurse, Haunt, ItemCurse, Flinch, Delusion, Sleep, Counter, SpiritSeal, Decoy, Key, Trap }

    public enum RndElement { Venus, Mars, Jupiter, Mercury }

    public enum LevelOption { Default, SetLevel, CappedLevel }

    public enum InventoryOption { Default, NoInventory }

    public enum DjinnOption { Unique, Any, NoDjinn }
    public enum EndlessMode { Default, Legacy };
    public enum BaseStatOption { Default, Average }

    public enum BaseStatManipulationOption { Default, NoIncrease }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ArchType { Warrior, Mage }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ChestQuality { Wooden, Normal, Silver, Gold, Adept, Daily }

    public enum Detail { None, Names, NameAndPrice }

    public enum TimeToActivate { beforeDamage, afterDamage };

    public enum RpsEnum { Rock, Paper, Scissors }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Element { Venus, Mars, Jupiter, Mercury, none };

    public enum RandomItemType { Any, Artifact, NonArtifact }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum TargetType { PartySelf, PartySingle, PartyAll, EnemyRange, EnemyAll }
    // 

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ItemType
    {
        Collectible,
        LongSword, Axe, Staff, LightBlade, Mace, Bow, Claw,
        Shield, Bracelet, Glove,
        HeavyArmor, Robe, LightArmor,
        Helmet, Hat, Circlet, Crown,
        UnderWear,
        Boots, Greave,
        Ring, Misc
    }

    public enum ItemRarity
    {
        Common, Uncommon, Rare, Legendary, Mythical, Unique
    }

    // Wooden: 100% Common
    // Norma: 50% Common, 50% uncommon
    // Silver: 40% Uncommon, 50% Rare, 10% Legendary
    // Gold: 60% Rare, 40% Legendary
    // Adept: 30% Rare, 65% Legendary, 5% Mythical

    public enum RoomVisibility { All, TeamA, TeamB, Private }

    public enum RankEnum { Level, Solo, Duo, Trio, Quad }

    public enum BattleDifficulty { Tutorial = 0, Easy = 1, Medium = 2, MediumRare = 3, Hard = 4, Adept = 5 };

    public enum ItemCategory
    {
        Weapon, ArmWear, ChestWear, HeadWear, UnderWear, FootWear, Accessoire, Other
    }
}