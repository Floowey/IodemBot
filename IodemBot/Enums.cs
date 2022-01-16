using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace IodemBot
{
    public enum DjinnDetail
    {
        None,
        Names
    }

    public enum Team
    {
        A,
        B
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Condition
    {
        Down,
        Poison,
        Venom,
        Seal,
        Stun,
        DeathCurse,
        Haunt,
        ItemCurse,
        Flinch,
        Delusion,
        Sleep,
        Counter,
        SpiritSeal,
        Decoy,
        Key,
        Trap
    }

    public enum LevelOption
    {
        Default,
        SetLevel,
        CappedLevel
    }

    public enum InventoryOption
    {
        Default,
        NoInventory
    }

    public enum DjinnOption
    {
        Unique,
        Any,
        NoDjinn
    }

    public enum EndlessMode
    {
        Default,
        Legacy
    }

    public enum BaseStatOption
    {
        Default,
        Average
    }

    public enum BaseStatManipulationOption
    {
        Default,
        NoIncrease
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ArchType
    {
        Warrior,
        Mage
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ChestQuality
    {
        Wooden,
        Normal,
        Silver,
        Gold,
        Adept,
        Daily
    }

    public enum Detail
    {
        None,
        Names,
        NameAndPrice
    }

    public enum TimeToActivate
    {
        BeforeDamage,
        AfterDamage
    }

    public enum RpsEnum
    {
        Rock,
        Paper,
        Scissors
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Element
    {
        Venus,
        Mars,
        Jupiter,
        Mercury,
        None
    }

    public enum RandomItemType
    {
        Any,
        Artifact,
        NonArtifact
    }

    [JsonConverter(typeof(StringEnumConverter))]
    [Flags]
    public enum TargetType
    {
        PartySelf = 0,
        PartySingle = 1,
        PartyAll = 2,
        EnemyRange = 4,
        EnemyAll = 8,
        OnParty = PartySelf | PartySingle | PartyAll,
        OnEnemy = EnemyRange | EnemyAll,
        NoAim = PartySelf | PartyAll | EnemyAll
    }

    //

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ItemType
    {
        Collectible,
        LongSword,
        Axe,
        Staff,
        LightBlade,
        Mace,
        Bow,
        Claw,
        Shield,
        Bracelet,
        Glove,
        HeavyArmor,
        Robe,
        LightArmor,
        Helmet,
        Hat,
        Circlet,
        Crown,
        UnderWear,
        Boots,
        Greave,
        Ring,
        Misc
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary,
        Mythical,
        Unique
    }

    // Wooden: 100% Common
    // Norma: 50% Common, 50% uncommon
    // Silver: 40% Uncommon, 50% Rare, 10% Legendary
    // Gold: 60% Rare, 40% Legendary
    // Adept: 30% Rare, 65% Legendary, 5% Mythical

    public enum RoomVisibility
    {
        All,
        TeamA,
        TeamB,
        Private
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum RankEnum
    {
        Level,
        LevelWeek,
        LevelMonth,
        Solo,
        Duo,
        Trio,
        Quad
    }

    public enum BattleDifficulty
    {
        Tutorial = 0,
        Easy = 1,
        Medium = 2,
        MediumRare = 3,
        Hard = 4,
        Adept = 5
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ItemCategory
    {
        Weapon,
        ArmWear,
        ChestWear,
        HeadWear,
        UnderWear,
        FootWear,
        Accessory,
        Other
    }
}