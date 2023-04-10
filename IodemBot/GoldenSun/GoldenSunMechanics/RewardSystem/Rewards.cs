using System;
using System.Collections.Generic;
using System.Linq;
using IodemBot.ColossoBattles;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class DefaultReward : Rewardable
    {
        public uint Coins { get; set; } = 0;
        public uint Xp { get; set; } = 0;
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

            var hasAllRequired = RequireTag.Where(s => !s.StartsWith('!')).All(userAccount.Tags.Contains); // Have all required Tags
            var hasRestricted = RequireTag.Where(s => s.StartsWith('!')).Select(s => s[1..]).Any(userAccount.Tags.Contains);// Have none of restricted Tags

            if (RequireTag.Any() && (!hasAllRequired || hasRestricted))
            {
                return "";
            }

            if (Obtainable > 0 && userAccount.Tags.Count(Tag.Equals) >= Obtainable)
            {
                return "";
            }
            var giveTag = true;
            userAccount.AddXp(Xp);

            if (userAccount.ClassSeries.Name == "Pirate Series")
                userAccount.Inv.AddBalance((uint)(Coins * 1.25));
            else
                userAccount.Inv.AddBalance(Coins);

            if (HasChest)
            {
                userAccount.Inv.AwardChest(Chest);
                awardLog.Add($"{userAccount.Name} found a {Emotes.GetIcon(Chest)} {Chest} chest!");
            }
            if (Item != "")
            {
                if (Item.StartsWith("GameTicket"))
                {
                    if (!uint.TryParse(Item[..^1], out uint tickets))
                        tickets = 1;
                    userAccount.Inv.GameTickets += tickets;
                }
                else
                {
                    var item = ItemDatabase.GetItem(Item);
                    if (userAccount.Inv.Add(item))
                    {
                        bool autoSold = false;
                        if (userAccount.Preferences.AutoSell.Contains(item.Rarity))
                            autoSold = userAccount.Inv.Sell(item.Name);
                        awardLog.Add($"{userAccount.Name} found a {item.Icon} {item.Name}{(autoSold ? " (Auto sold)" : "")}!");
                    }
                    else
                    {
                        giveTag = false;
                    }
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
                            "In battle you can use a djinn to unleash its powers. After that it will go into \"Ready\" mode. From there you can use it to call a summon. After summoning, a djinn will take some turns to recover. " +
                            "You can also team up with other people to use a higher number of djinn in even more powerful summon sequences! " +
                            "Make sure to check `i!help DjinnAndSummons` for a full list of the commands related to djinn!");
                    }

                    if (djinn.IsEvent && userAccount.DjinnPocket.Djinn.Count(d => d.IsEvent) == 1)
                    {
                        awardLog.Add("Congratulations, You have found an **Event Djinni**! They are custom made djinni, only available within the event, as a small trinket for your participation. " +
                            "They behave differently to other djinn, in that they will not count towards your Djinn Pocket limit or any class upgrades, " +
                            "however they will carry over if you decide to reset your game :)" +
                            "(Event Djinn will not be allowed in any upcoming tournaments.)");
                    }

                    if (userAccount.DjinnPocket.Djinn.Count == userAccount.DjinnPocket.PocketSize)
                    {
                        awardLog.Add("Attention! Your Djinn Pocket has reached its limit. " +
                            "In order to further obtain djinn, you must either make space by releasing djinn or upgrading it using `i!upgradedjinn`!");
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
                bool isShiny = Global.RandomNumber(0, 128 - userAccount.DjinnBadLuck < 0 ? 0 : 128 - userAccount.DjinnBadLuck) <= 0;
                if (!isShiny && userAccount.DjinnPocket.Djinn.Any(d => d.Djinnname == djinn.Djinnname))
                {
                    djinn = DjinnAndSummonsDatabase.GetRandomDjinn(element);
                }
                else if (isShiny)
                {
                    while (userAccount.DjinnPocket.Djinn.Any(d => d.Djinnname == djinn.Djinnname && d.IsShiny))
                    {
                        djinn = DjinnAndSummonsDatabase.GetRandomDjinn(element);
                    }
                }
                djinn.IsShiny = isShiny && djinn.CanBeShiny;
                djinn.UpdateMove();
                if (userAccount.DjinnPocket.AddDjinn(djinn))
                {
                    awardLog.Add($"{userAccount.Name} found the {djinn.Element} djinni {djinn.Emote} {djinn.Name}!");
                    if (userAccount.DjinnPocket.Djinn.Count == 1)
                    {
                        awardLog.Add($"You have found your first djinni, the {djinn.Element} djinni {djinn.Emote} {djinn.Name}. " +
                            $"To view what it can do, use the djinninfo command `i!di {djinn.Name}` and to take it with you on your journey, use `i!djinn take {djinn.Name}` as long as it matches one of your classes elements. " +
                            "In battle you can use a djinn to unleash its powers. After that it will go into \"Ready\" mode. From there you can use it to call a summon. After summoning, a djinn will take some turns to recover. " +
                            "You can also team up with other people to use a higher number of djinn in even more powerful summon sequences! " +
                            "Find more djinn by battling them in various towns and locations, and with some luck they will join you." +
                            "Make sure to check `i!help DjinnAndSummons` for a full list of the commands related to djinn!");
                    }

                    if (userAccount.DjinnPocket.Djinn.Count == userAccount.DjinnPocket.PocketSize)
                    {
                        awardLog.Add("Attention! Your Djinn Pocket has reached its limit. " +
                            "In order to further obtain djinn, you must either make space by releasing djinn or upgrading it using `i!upgradedjinn`!");
                    }

                    if (djinn.IsShiny)
                    {
                        userAccount.DjinnBadLuck = 0;
                        userAccount.ServerStats.ShinyDjinnObtained++;
                        userAccount.ServerStats.TotalShinyChances++;
                    }
                    else if (djinn.CanBeShiny)
                    {
                        userAccount.ServerStats.TotalShinyChances++;
                        userAccount.DjinnBadLuck++;
                    }
                    userAccount.ServerStats.DjinnObtained++;
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

            if (!Message.IsNullOrEmpty())
            {
                awardLog.Add(string.Format(Message, userAccount.Name));
            }
            if (giveTag && Tag != "")
            {
                if (Tag.StartsWith('-'))
                {
                    if (Tag.EndsWith('*'))
                        userAccount.Tags.RemoveAll(t => t.StartsWith(Tag[1..^1]));
                    else
                        userAccount.Tags.Remove(Tag[1..]);

                    if (Tag.StartsWith("-Oath"))
                        userAccount.Oaths.ActiveOaths.Remove(Enum.Parse<Oath>(Tag[5..]));
                }
                else
                {
                    userAccount.Tags.Add(Tag);
                    if (Tag.StartsWith("Oath"))
                        userAccount.Oaths.ActiveOaths.Add(Enum.Parse<Oath>(Tag[4..]));
                }
            }
            return string.Join("\n", awardLog);
        }
    }
}