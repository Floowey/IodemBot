using IodemBot.Core.UserManagement;
using IodemBot.Modules.ColossoBattles;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class DefaultReward : Rewardable
    {
        public uint coins = 0;
        public uint xp = 0;
        public string Item = "";

        public bool HasChest = false;

        [JsonConverter(typeof(StringEnumConverter))]
        public ChestQuality Chest { get; set; } = ChestQuality.Wooden;

        public string Dungeon = "";
        public string Djinn = "";
        public string Summon = "";

        public override string Award(UserAccount userAccount)
        {
            List<string> awardLog = new List<string>();
            userAccount.XP += xp;
            userAccount.Inv.AddBalance(coins);

            if (HasChest)
            {
                userAccount.Inv.AwardChest(Chest);
                awardLog.Add($"{userAccount.Name} found a {Inventory.ChestIcons[Chest]} {Chest} chest!");
            }
            if (Item != "")
            {
                var item = ItemDatabase.GetItem(Item);
                userAccount.Inv.Add(Item);
                awardLog.Add($"{userAccount.Name} found a found a {item.Icon} {item.Name}!");
            }

            if (EnemiesDatabase.TryGetDungeon(Dungeon, out var dungeon))
            {
                userAccount.Dungeons.Add(dungeon.Name);
                awardLog.Add($"{userAccount.Name} found a found a {(dungeon.IsOneTimeOnly ? "<:dungeonkey:606237382047694919> Key" : "<:mapclosed:606236181486632985> Map")} to {dungeon.Name}!");
            }

            if (DjinnAndSummonsDatabase.TryGetDjinn(Djinn, out var djinn))
            {
                if (!userAccount.DjinnPocket.djinn.Any(d => d.Djinnname == djinn.Djinnname))
                {
                    userAccount.DjinnPocket.AddDjinn(djinn);
                    awardLog.Add($"{userAccount.Name} found the djinn a {djinn.Emote} {djinn.Name}!");
                }
            }
            else if (Enum.TryParse<Element>(Djinn, out var element))
            {
                djinn = DjinnAndSummonsDatabase.GetRandomDjinn(element);
                djinn.IsShiny = Global.Random.Next(0, 128) == 0;
                djinn.UpdateMove();
                userAccount.DjinnPocket.AddDjinn(djinn);
                awardLog.Add($"{userAccount.Name} found a the djinn a {djinn.Emote} {djinn.Name}!");
            }

            if (DjinnAndSummonsDatabase.TryGetSummon(Summon, out var summon))
            {
                if (!userAccount.DjinnPocket.summons.Contains(summon))
                {
                    userAccount.DjinnPocket.AddSummon(summon);
                    awardLog.Add($"{userAccount.Name} found the summon tablet for {summon.Emote} {summon.Name}!");
                }
            }
            return string.Join("\n", awardLog);
        }
    }
}