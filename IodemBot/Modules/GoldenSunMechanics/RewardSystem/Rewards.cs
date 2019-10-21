using IodemBot.Core.UserManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace IodemBot.Modules.GoldenSunMechanics
{
    internal class ChestReward : Rewardable
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ChestQuality Chest { get; set; } = ChestQuality.Wooden;

        public override string Award(UserAccount userAccount)
        {
            userAccount.Inv.AwardChest(Chest);
            return $"{userAccount.Name} found a {Inventory.ChestIcons[Chest]} {Chest} chest!";
        }
    }

    public class DefaultReward : Rewardable
    {
        public uint coins = 0;
        public uint xp = 0;

        public override string Award(UserAccount userAccount)
        {
            userAccount.XP += xp;
            userAccount.Inv.AddBalance(coins);
            return null;
        }
    }

    public class ItemReward : Rewardable
    {
        public string Item = "";

        public override string Award(UserAccount userAccount)
        {
            var item = ItemDatabase.GetItem(Item);
            if (item != null)
            {
                userAccount.Inv.Add(Item);
                return $"{userAccount.Name} found a found a {item.Icon} {item.Name}!";
            }
            return null;
        }
    }

    public class DungeonReward : Rewardable
    {
        public string Dungeon = "";

        public override string Award(UserAccount userAccount)
        {
            var dungeon = ColossoBattles.EnemiesDatabase.GetDungeon(Dungeon);
            if (dungeon != null)
            {
                userAccount.Dungeons.Add(dungeon.Name);
                return $"{userAccount.Name} found a found a {(dungeon.IsOneTimeOnly ? "<:dungeonkey:606237382047694919> Key" : "<:mapclosed:606236181486632985> Map")} to {dungeon.Name}!";
            }
            return null;
        }
    }

    public class DjinnReward : Rewardable
    {
        public string Djinn = "";

        public override string Award(UserAccount userAccount)
        {
            throw new NotImplementedException("DjinnReward");
        }
    }
}