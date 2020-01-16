using IodemBot.Core.UserManagement;
using IodemBot.Modules.ColossoBattles;
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

        public ChestQuality Chest { get; set; } = ChestQuality.Wooden;

        public string Dungeon = "";
        public string Djinn = "";
        public string Summon = "";
        public string Message = "";

        public override string Award(UserAccount userAccount)
        {
            List<string> awardLog = new List<string>();

            if (RequireTag.Count > 0 && !RequireTag.All(t => userAccount.Tags.Contains(t)))
            {
                return "";
            }

            if (Obtainable > 0 && userAccount.Tags.Count(r => r.Equals(Tag)) >= Obtainable)
            {
                return "";
            }
            var giveTag = true;
            userAccount.AddXp(xp);
            userAccount.Inv.AddBalance(coins);

            if (HasChest)
            {
                userAccount.Inv.AwardChest(Chest);
                awardLog.Add($"{userAccount.Name} found a {Inventory.ChestIcons[Chest]} {Chest} chest!");
            }
            if (Item != "")
            {
                var item = ItemDatabase.GetItem(Item);
                if (userAccount.Inv.Add(Item))
                {
                    awardLog.Add($"{userAccount.Name} found a {item.Icon} {item.Name}!");
                }
                else
                {
                    giveTag = false;
                }
            }

            if (EnemiesDatabase.TryGetDungeon(Dungeon, out var dungeon))
            {
                if (!userAccount.Dungeons.Contains(dungeon.Name))
                {
                    userAccount.Dungeons.Add(dungeon.Name);
                    awardLog.Add($"{userAccount.Name} found a {(dungeon.IsOneTimeOnly ? "<:dungeonkey:606237382047694919> Key" : "<:mapclosed:606236181486632985> Map")} to {dungeon.Name}!");
                }
            }

            if (DjinnAndSummonsDatabase.TryGetDjinn(Djinn, out var djinn))
            {
                if (!userAccount.DjinnPocket.djinn.Any(d => d.Djinnname == djinn.Djinnname))
                {
                    if (userAccount.DjinnPocket.AddDjinn(djinn))
                    {
                        awardLog.Add($"{userAccount.Name} found the {djinn.Element} djinni {djinn.Emote} {djinn.Name}!");
                    }
                    else
                    {
                        giveTag = false;
                    }
                }
            }
            else if (Enum.TryParse<Element>(Djinn, out var element))
            {
                djinn = DjinnAndSummonsDatabase.GetRandomDjinn(element);
                djinn.IsShiny = Global.Random.Next(0, 128) == 0;
                djinn.UpdateMove();
                if (userAccount.DjinnPocket.AddDjinn(djinn))
                {
                    awardLog.Add($"{userAccount.Name} found the {djinn.Element} djinni {djinn.Emote} {djinn.Name}!");
                }
                else
                {
                    giveTag = false;
                }
            }

            if (DjinnAndSummonsDatabase.TryGetSummon(Summon, out var summon))
            {
                if (!userAccount.DjinnPocket.summons.Contains(summon))
                {
                    userAccount.DjinnPocket.AddSummon(summon);
                    awardLog.Add($"{userAccount.Name} found the summon tablet for {summon.Emote} {summon.Name}!");
                }
            }

            if (Message != "")
            {
                awardLog.Add(string.Format(Message, userAccount.Name));
            }
            if (giveTag && Tag != "")
            {
                userAccount.Tags.Add(Tag);
            }
            return string.Join("\n", awardLog);
        }
    }
}