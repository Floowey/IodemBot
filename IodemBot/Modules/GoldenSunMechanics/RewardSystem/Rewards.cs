using System;
using System.Collections.Generic;
using System.Linq;
using IodemBot.Core.UserManagement;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class DefaultReward : Rewardable
    {
        public uint coins { get; set; } = 0;
        public uint xp { get; set; } = 0;
        public string Item { get; set; } = "";

        public bool HasChest { get; set; } = false;

        public ChestQuality Chest { get; set; } = ChestQuality.Wooden;

        public string Dungeon { get; set; } = "";
        public string Djinn { get; set; } = "";
        public string Summon { get; set; } = "";
        public string Message { get; set; } = "";

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
                if (userAccount.DjinnPocket.AddDjinn(djinn))
                {
                    awardLog.Add($"{userAccount.Name} found the {djinn.Element} djinni {djinn.Emote} {djinn.Name}!");
                    if (userAccount.DjinnPocket.Djinn.Count == 1)
                    {
                        awardLog.Add($"You have found your first djinni, the {djinn.Element} djinni {djinn.Emote} {djinn.Name}. " +
                            $"To view what it can do, use the djinninfo command `i!di {djinn.Name}` and to take it with you on your journey, use `i!djinn take {djinn.Name}`. " +
                            $"In battle you can use a djinn to unleash its powers. After that it will go into \"Ready\" mode. From there you can use it to call a summon. After summoning, a djinn will take some turns to recover. " +
                            $"You can also team up with other people to use a higher number of djinn in even more powerful summon sequences! " +
                            $"Make sure to check `i!help DjinnAndSummons` for a full list of the commands related to djinn!");
                    }
                }
                else
                {
                    giveTag = false;
                }
            }
            else if (Enum.TryParse<Element>(Djinn, out var element))
            {
                djinn = DjinnAndSummonsDatabase.GetRandomDjinn(element);
                var isShiny = Global.RandomNumber(0, 128) == 0;
                if (!isShiny && userAccount.DjinnPocket.Djinn.Any(d => d.Djinnname == djinn.Djinnname))
                {
                    djinn = DjinnAndSummonsDatabase.GetRandomDjinn(element);
                }
                djinn.IsShiny = isShiny;
                djinn.UpdateMove();
                if (userAccount.DjinnPocket.AddDjinn(djinn))
                {
                    awardLog.Add($"{userAccount.Name} found the {djinn.Element} djinni {djinn.Emote} {djinn.Name}!");
                    if (userAccount.DjinnPocket.Djinn.Count == 1)
                    {
                        awardLog.Add($"You have found your first djinni, the {djinn.Element} djinni {djinn.Emote} {djinn.Name}. " +
                            $"To view what it can do, use the djinninfo command `i!di {djinn.Name}` and to take it with you on your journey, use `i!djinn take {djinn.Name}` as long as it matches one of your classes elements. " +
                            $"In battle you can use a djinn to unleash its powers. After that it will go into \"Ready\" mode. From there you can use it to call a summon. After summoning, a djinn will take some turns to recover. " +
                            $"You can also team up with other people to use a higher number of djinn in even more powerful summon sequences! " +
                            $"Find more djinn by battling them in various towns and locations, and with some luck they will join you." +
                            $"Make sure to check `i!help DjinnAndSummons` for a full list of the commands related to djinn!");
                    }
                }
                else
                {
                    giveTag = false;
                }
            }

            if (DjinnAndSummonsDatabase.TryGetSummon(Summon, out var summon))
            {
                if (!userAccount.DjinnPocket.Summons.Contains(summon))
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