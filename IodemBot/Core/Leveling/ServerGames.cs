using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using IodemBot.ColossoBattles;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.Core.Leveling
{
    internal static class ServerGames
    {
        internal static string BattleFile => $"Logs/Battles_{DateTime.Now:yyyy-MM-dd}.log";

        internal static async void UserWonColosso(SocketGuildUser user, IMessageChannel channel)
        {
            var userAccount = EntityConverter.ConvertUser(user);
            var oldLevel = userAccount.LevelNumber;
            userAccount.AddXp((uint)Global.RandomNumber(40, 70));
            var newLevel = userAccount.LevelNumber;

            userAccount.ServerStats.ColossoWins++;
            userAccount.ServerStats.ColossoStreak++;
            if (userAccount.ServerStats.ColossoStreak > userAccount.ServerStats.ColossoHighestStreak)
                userAccount.ServerStats.ColossoHighestStreak = userAccount.ServerStats.ColossoStreak;

            UserAccountProvider.StoreUser(userAccount);
            if (oldLevel != newLevel) Leveling.LevelUp(userAccount, user, channel);
            await Task.CompletedTask;
        }

        internal static async void UserLostColosso(SocketGuildUser user, IMessageChannel channel)
        {
            var userAccount = EntityConverter.ConvertUser(user);
            var oldLevel = userAccount.LevelNumber;
            userAccount.AddXp((uint)Global.RandomNumber(1, 10));
            var newLevel = userAccount.LevelNumber;
            userAccount.ServerStats.ColossoStreak = 0;
            UserAccountProvider.StoreUser(userAccount);

            if (oldLevel != newLevel) Leveling.LevelUp(userAccount, user, channel);
            await Task.CompletedTask;
        }

        internal static async Task UserWonBattle(UserAccount userAccount, List<Rewardable> rewards,
            BattleStats battleStats, ITextChannel lobbyChannel, ITextChannel battleChannel)
        {
            var oldLevel = userAccount.LevelNumber;

            userAccount.BattleStats += battleStats;
            _ = UnlockBattleClasses(userAccount, lobbyChannel);

            var awardStrings = rewards.Select(f => f.Award(userAccount)).Where(s => !s.IsNullOrEmpty()).ToList();
            if (awardStrings.Any()) _ = WriteAndDeleteRewards(awardStrings, battleChannel);

            userAccount.ServerStats.ColossoWins++;
            userAccount.ServerStats.ColossoStreak++;
            userAccount.ServerStats.ColossoHighestStreak = Math.Max(userAccount.ServerStats.ColossoHighestStreak,
                userAccount.ServerStats.ColossoStreak);

            UserAccountProvider.StoreUser(userAccount);
            var newLevel = userAccount.LevelNumber;
            if (oldLevel != newLevel)
            {
                var user = (SocketGuildUser)await lobbyChannel.GetUserAsync(userAccount
                    .Id); // Where(s => s. == userAccount.ID).First();
                Leveling.LevelUp(userAccount, user, (SocketTextChannel)lobbyChannel);
            }

            await Task.CompletedTask;
        }

        private static async Task WriteAndDeleteRewards(List<string> text, ITextChannel channel)
        {
            if (text.Count == 0) return;

            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithDescription($"{string.Join("\n", text)}");
            _ = GuildSettings.GetGuildSettings(channel.Guild).CommandChannel.SendMessageAsync("", false, embed.Build());
            var msg = await channel.SendMessageAsync("", false, embed.Build());
            await Task.Delay(5000);
            _ = msg.DeleteAsync();
        }

        private static async Task UnlockBattleClasses(UserAccount userAccount, ITextChannel channel)
        {
            var bs = userAccount.BattleStats;
            if (userAccount.ServerStats.ColossoWins >= 20)
                await GoldenSunCommands.AwardClassSeries("Brute Series", userAccount, channel);

            if (bs.KillsByHand >= 161) await GoldenSunCommands.AwardClassSeries("Samurai Series", userAccount, channel);

            if (bs.DamageDealt >= 222222)
                await GoldenSunCommands.AwardClassSeries("Ninja Series", userAccount, channel);

            if (bs.SoloBattles >= 100) await GoldenSunCommands.AwardClassSeries("Ranger Series", userAccount, channel);

            if (bs.TotalTeamMates >= 100)
                await GoldenSunCommands.AwardClassSeries("Dragoon Series", userAccount, channel);

            if (bs.HPhealed >= 222222)
                await GoldenSunCommands.AwardClassSeries("White Mage Series", userAccount, channel);

            if (bs.ItemActivations >= 10)
                await GoldenSunCommands.AwardClassSeries("Prospector Series", userAccount, channel);

            if (bs.HighestDamage >= 500)
                await GoldenSunCommands.AwardClassSeries("Tribalist Series", userAccount, channel);

            if (bs.PPUsed >= 1000)
                await GoldenSunCommands.AwardClassSeries("Fakir Series", userAccount, channel);

            if (bs.DamageTanked >= 10000)
                await GoldenSunCommands.AwardClassSeries("Toa Series", userAccount, channel);

            if (bs.Revives >= 25) await GoldenSunCommands.AwardClassSeries("Medium Series", userAccount, channel);
        }

        internal static async Task UserWonPvP(UserAccount avatar, ITextChannel lobbyChannel, int numberOfWinners,
            int numberOfLosers)
        {
            _ = GoldenSunCommands.AwardClassSeries("Swordsman Series", avatar, lobbyChannel);
            var csvline =
                $"{DateTime.Now:s},PvP,{numberOfWinners}vs{numberOfLosers},{avatar.Name}{Environment.NewLine}";
            File.AppendAllText(BattleFile, csvline);
            await Task.CompletedTask;
        }

        internal static async Task UserWonEndless(UserAccount avatar, int winsInARow, EndlessMode mode, int nOfPlayers,
            string teamMatesNames)
        {
            if (mode == EndlessMode.Default)
                avatar.ServerStats.EndlessStreak.AddStreak(winsInARow, nOfPlayers, teamMatesNames);
            else
                avatar.ServerStats.LegacyStreak.AddStreak(winsInARow, nOfPlayers, teamMatesNames);
            UserAccountProvider.StoreUser(avatar);
            await Task.CompletedTask;
        }

        internal static async Task UserFinishedEndless(UserAccount avatar, int winsInARow, EndlessMode mode)
        {
            avatar.Inv.GameTickets += Math.Max((uint)winsInARow, (uint)(winsInARow * winsInARow / 12));
            UserAccountProvider.StoreUser(avatar);
            var csvline = $"{DateTime.Now:s},Endless {mode},{winsInARow},{avatar.Name}{Environment.NewLine}";
            File.AppendAllText(BattleFile, csvline);
            await Task.CompletedTask;
        }

        internal static async Task UserWonDungeon(UserAccount avatar, EnemiesDatabase.Dungeon dungeon,
            ITextChannel channel)
        {
            avatar.ServerStats.DungeonsCompleted++;
            if (avatar.ServerStats.LastDungeon == dungeon.Name)
            {
                avatar.ServerStats.SameDungeonInARow++;
                if (avatar.ServerStats.SameDungeonInARow >= 5)
                    _ = GoldenSunCommands.AwardClassSeries("Hermit Series", avatar, channel);
            }

            avatar.ServerStats.LastDungeon = dungeon.Name;

            if (dungeon.Name == "Mercury Lighthouse")
                _ = GoldenSunCommands.AwardClassSeries("Aqua Pilgrim Series", avatar, channel);

            if (dungeon.Name == "Karagol Sea")
                _ = GoldenSunCommands.AwardClassSeries("Viking Series", avatar, channel);

            //Unlock Crusader
            if (avatar.Dungeons.Count >= 6)
                _ = GoldenSunCommands.AwardClassSeries("Crusader Series", avatar, channel);

            if (avatar.ServerStats.DungeonsCompleted >= 12)
                _ = GoldenSunCommands.AwardClassSeries("Air Pilgrim Series", avatar, channel);

            var passivesBefore = avatar.Passives.UnlockedPassives.ToList();
            var PassiveLevelsBefore = avatar.Passives.UnlockedPassives.Select(p => Passives.GetPassiveLevel(p, avatar.Oaths)).ToArray();

            if (avatar.Oaths.ActiveOaths.Any())
            {
                string dungeonToComplete = avatar.Oaths.IsOathOfElementActive() ? " I" : "Vault";
                //string dungeonToComplete = avatar.Oaths.IsOathOfElementActive() ? " IV" : "Venus Lighthouse";
                if (dungeon.Name.EndsWith(dungeonToComplete))
                {
                    var oaths = avatar.Oaths.ActiveOaths.ToList();
                    var oafLevel = avatar.Oaths.ActiveOaths.Contains(Oath.Oaf) ? avatar.LevelNumber : 0;
                    var elementOath = avatar.Oaths.IsOathOfElementActive();
                    try
                    {
                        avatar.Oaths.CompleteOaths();
                    }
                    catch (Exception e)
                    {
                        Console.Write(e);
                    }

                    var embed = new EmbedBuilder();
                    embed.WithColor(Colors.Get("Iodem"));
                    embed.WithTitle("Oaths fulfilled!");
                    embed.WithDescription($"Hereby {avatar.Name} has ceremoniously fulfilled the following Oaths:\n" +
                        $"{string.Join("\n", oaths.Select(o => $"Oath of {o}"))}");

                    if (oafLevel > 0 && oafLevel <= 50)
                        avatar.TrophyCase.Trophies.Add(new Trophy()
                        {
                            Text = $"Awarded for completing Oath of the Oaf by completing {(elementOath ? "Path IV" : "Vault")} at level {oafLevel}.",
                            Icon = elementOath ? "<:Krakden:576856312500060161>" : "<:Nut:548636122402783243>",
                            ObtainedOn = DateTime.Now
                        });

                    _ = channel.SendMessageAsync(embed: embed.Build());
                }
            }

            if (dungeon.Name.EndsWith(" I")) // IV
            {
                var el = avatar.Element;
                var unlockedPassives = Passives.AllPassives.Except(avatar.Passives.UnlockedPassives).Where(p => p.elements.Contains(el)).ToList();

                var embed = new EmbedBuilder();
                embed.WithColor(Colors.Get(el.ToString()));
                embed.WithTitle("Passive Initiatives mastered");
                if (unlockedPassives.Any())
                {
                    avatar.Passives.AddPassive(unlockedPassives.ToArray());
                    embed.WithDescription($"The power of {el} flushes through your soul. The following Passive Initiatives are now newly available to you:\n" +
                        $"{string.Join("\n", unlockedPassives.Select(p => $"{p.Name} ({Passives.GetPassiveLevel(p, avatar.Oaths)})"))}");
                }

                var PassiveLevelsAfter = avatar.Passives.UnlockedPassives.Select(p => Passives.GetPassiveLevel(p, avatar.Oaths)).ToArray();
                if (!Enumerable.SequenceEqual(PassiveLevelsBefore, PassiveLevelsAfter))
                {
                    StringBuilder msg = new();
                    for (int i = 0; i < passivesBefore.Count; i++)
                    {
                        if (PassiveLevelsBefore[i] != PassiveLevelsAfter[i])
                        {
                            msg.Append($"{passivesBefore[i].Name} ({PassiveLevelsBefore[i]}) -> ({PassiveLevelsAfter[i]})");
                        }
                    }
                    embed.AddField("Passives Upgraded!", msg);
                }
                if (embed.Fields.Any() || !embed.Description.IsNullOrEmpty())
                {
                    _ = channel.SendMessageAsync(embed: embed.Build());
                }
            }

            UserAccountProvider.StoreUser(avatar);
            var csvline = $"{DateTime.Now:s},Dungeon,{dungeon.Name},{avatar.Name}{Environment.NewLine}";
            File.AppendAllText(BattleFile, csvline);

            await Task.CompletedTask;
        }

        internal static async Task UserWonSingleBattle(UserAccount avatar, BattleDifficulty difficulty)
        {
            avatar.Inv.GameTickets += (uint)difficulty;
            UserAccountProvider.StoreUser(avatar);
            var csvline = $"{DateTime.Now:s},Single,{difficulty},{avatar.Name}{Environment.NewLine}";
            File.AppendAllText(BattleFile, csvline);
            await Task.CompletedTask;
        }

        internal static async Task UserSentCommand(IUser user, IMessageChannel channel)
        {
            var userAccount = EntityConverter.ConvertUser(user);
            userAccount.ServerStats.CommandsUsed++;
            UserAccountProvider.StoreUser(userAccount);
            if (userAccount.ServerStats.CommandsUsed >= 50)
                await GoldenSunCommands.AwardClassSeries("Scrapper Series", user, channel);
        }

        internal static async Task UserWonRps(SocketGuildUser user, SocketTextChannel channel)
        {
            var userAccount = EntityConverter.ConvertUser(user);
            userAccount.ServerStats.RpsWins++;
            userAccount.ServerStats.RpsStreak++;
            UserAccountProvider.StoreUser(userAccount);

            if (userAccount.ServerStats.RpsWins >= 3)
                await GoldenSunCommands.AwardClassSeries("Aqua Seer Series", user, channel);
        }

        internal static void UserDidNotWinRps(SocketGuildUser user)
        {
            var userAccount = EntityConverter.ConvertUser(user);
            userAccount.ServerStats.RpsStreak = 0;
            UserAccountProvider.StoreUser(userAccount);
        }

        internal static async Task UserLostBattle(UserAccount userAccount, ITextChannel battleChannel)
        {
            var oldLevel = userAccount.LevelNumber;
            userAccount.AddXp((uint)Global.RandomNumber(1, 10));
            var newLevel = userAccount.LevelNumber;

            userAccount.ServerStats.ColossoStreak = 0;

            UserAccountProvider.StoreUser(userAccount);
            if (oldLevel != newLevel)
            {
                var user = (SocketGuildUser)await battleChannel.GetUserAsync(userAccount.Id); // Where(s => s. == userAccount.ID).First();
                Leveling.LevelUp(userAccount, user, (SocketTextChannel)battleChannel);
            }

            await Task.CompletedTask;
        }

        internal static async Task UserLookedUpPsynergy(SocketGuildUser user, SocketTextChannel channel)
        {
            var userAccount = EntityConverter.ConvertUser(user);
            userAccount.ServerStats.LookedUpPsynergy++;
            UserAccountProvider.StoreUser(userAccount);

            if (userAccount.ServerStats.LookedUpPsynergy >= 14)
                _ = GoldenSunCommands.AwardClassSeries("Apprentice Series", user, channel);
            await Task.CompletedTask;
        }

        internal static async Task UserLookedUpClass(SocketGuildUser user, SocketTextChannel channel)
        {
            var userAccount = EntityConverter.ConvertUser(user);
            userAccount.ServerStats.LookedUpClass++;
            UserAccountProvider.StoreUser(userAccount);

            if (userAccount.ServerStats.LookedUpClass >= 10)
                _ = GoldenSunCommands.AwardClassSeries("Page Series", user, channel);
            await Task.CompletedTask;
        }

        internal static async Task UserLookedUpItem(SocketGuildUser user, SocketTextChannel channel)
        {
            var userAccount = EntityConverter.ConvertUser(user);
            userAccount.ServerStats.LookedUpItem++;
            UserAccountProvider.StoreUser(userAccount);

            if (userAccount.ServerStats.LookedUpItem >= 20)
                _ = GoldenSunCommands.AwardClassSeries("Scrapper Series", user, channel);
            await Task.CompletedTask;
        }
    }
}