using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules;
using IodemBot.Modules.ColossoBattles;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.Core.Leveling
{
    internal static class ServerGames
    {
        internal static string BattleFile { get => $"Logs/Battles_{DateTime.Now:yyyy-MM-dd}.log"; }
        internal static async void UserWonColosso(SocketGuildUser user, IMessageChannel channel)
        {
            var userAccount = EntityConverter.ConvertUser(user);
            uint oldLevel = userAccount.LevelNumber;
            userAccount.AddXp((uint)(new Random()).Next(40, 70));
            uint newLevel = userAccount.LevelNumber;


            userAccount.ServerStats.ColossoWins++;
            userAccount.ServerStats.ColossoStreak++;
            if (userAccount.ServerStats.ColossoStreak > userAccount.ServerStats.ColossoHighestStreak)
            {
                userAccount.ServerStats.ColossoHighestStreak = userAccount.ServerStats.ColossoStreak;
            }

            UserAccountProvider.StoreUser(userAccount);
            if (oldLevel != newLevel)
            {
                Leveling.LevelUp(userAccount, user, channel);
            }
            await Task.CompletedTask;
        }

        internal static async void UserLostColosso(SocketGuildUser user, IMessageChannel channel)
        {
            var userAccount = EntityConverter.ConvertUser(user);
            uint oldLevel = userAccount.LevelNumber;
            userAccount.AddXp((uint)(new Random()).Next(1, 10));
            uint newLevel = userAccount.LevelNumber;
            userAccount.ServerStats.ColossoStreak = 0;
            UserAccountProvider.StoreUser(userAccount);

            if (oldLevel != newLevel)
            {
                Leveling.LevelUp(userAccount, user, channel);
            }
            await Task.CompletedTask;
        }

        internal static async Task UserWonBattle(UserAccount userAccount, List<Rewardable> rewards, BattleStats battleStats, ITextChannel lobbyChannel, ITextChannel battleChannel)
        {
            uint oldLevel = userAccount.LevelNumber;

            userAccount.BattleStats += battleStats;
            var bs = userAccount.BattleStats;
            _ = UnlockBattleClasses(userAccount, lobbyChannel);

            var awardStrings = rewards.Select(f => f.Award(userAccount)).Where(s => !s.IsNullOrEmpty()).ToList();
            if (awardStrings.Count() > 0)
            {
                _ = WriteAndDeleteRewards(awardStrings, battleChannel);
            }

            userAccount.ServerStats.ColossoWins++;
            userAccount.ServerStats.ColossoStreak++;
            userAccount.ServerStats.ColossoHighestStreak = Math.Max(userAccount.ServerStats.ColossoHighestStreak, userAccount.ServerStats.ColossoStreak);

            UserAccountProvider.StoreUser(userAccount);
            uint newLevel = userAccount.LevelNumber;
            if (oldLevel != newLevel)
            {
                var user = (SocketGuildUser)await lobbyChannel.GetUserAsync(userAccount.ID); // Where(s => s. == userAccount.ID).First();
                Leveling.LevelUp(userAccount, user, (SocketTextChannel)lobbyChannel);
            }

            await Task.CompletedTask;
        }

        private static async Task WriteAndDeleteRewards(List<string> text, ITextChannel channel)
        {
            if (text.Count == 0)
            {
                return;
            }

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
            {
                await GoldenSunCommands.AwardClassSeries("Brute Series", userAccount, channel);
            }

            if (bs.KillsByHand >= 161)
            {
                await GoldenSunCommands.AwardClassSeries("Samurai Series", userAccount, channel);
            }

            if (bs.DamageDealt >= 222222)
            {
                await GoldenSunCommands.AwardClassSeries("Ninja Series", userAccount, channel);
            }

            if (bs.SoloBattles >= 100)
            {
                await GoldenSunCommands.AwardClassSeries("Ranger Series", userAccount, channel);
            }

            if (bs.TotalTeamMates >= 100)
            {
                await GoldenSunCommands.AwardClassSeries("Dragoon Series", userAccount, channel);
            }

            if (bs.HPhealed >= 222222)
            {
                await GoldenSunCommands.AwardClassSeries("White Mage Series", userAccount, channel);
            }

            if (bs.Revives >= 25)
            {
                await GoldenSunCommands.AwardClassSeries("Medium Series", userAccount, channel);
            }
        }

        internal static async Task UserWonPvP(UserAccount avatar, ITextChannel lobbyChannel, int numberOfWinners, int numberOfLosers)
        {
            _ = GoldenSunCommands.AwardClassSeries("Swordsman Series", avatar, lobbyChannel);
            string csvline = $"{DateTime.Now:s},PvP,{numberOfWinners}vs{numberOfLosers},{avatar.Name}{Environment.NewLine}";
            File.AppendAllText(BattleFile, csvline);
            await Task.CompletedTask;
        }

        internal static async Task UserWonEndless(UserAccount avatar, int winsInARow, EndlessMode mode, int nOfPlayers, string TeamMatesNames)
        {
            if (mode == EndlessMode.Default)
            {
                avatar.ServerStats.EndlessStreak.AddStreak(winsInARow, nOfPlayers, TeamMatesNames);
            }
            else
            {
                avatar.ServerStats.LegacyStreak.AddStreak(winsInARow, nOfPlayers, TeamMatesNames);
            }
            UserAccountProvider.StoreUser(avatar);
            await Task.CompletedTask;
        }

        internal static async Task UserFinishedEndless(UserAccount avatar, int winsInARow, EndlessMode mode)
        {
            string csvline = $"{DateTime.Now:s},Endless {mode},{winsInARow},{avatar.Name}{Environment.NewLine}";
            File.AppendAllText(BattleFile, csvline);
            await Task.CompletedTask;
        }

        internal static async Task UserWonDungeon(UserAccount avatar, EnemiesDatabase.Dungeon dungeon, ITextChannel channel)
        {
            avatar.ServerStats.DungeonsCompleted++;
            if (avatar.ServerStats.LastDungeon == dungeon.Name)
            {
                avatar.ServerStats.SameDungeonInARow++;
                if (avatar.ServerStats.SameDungeonInARow >= 5)
                {
                    _ = GoldenSunCommands.AwardClassSeries("Hermit Series", avatar, channel);
                }
            }
            avatar.ServerStats.LastDungeon = dungeon.Name;
            UserAccountProvider.StoreUser(avatar);
            if (dungeon.Name == "Mercury Lighthouse")
            {
                _ = GoldenSunCommands.AwardClassSeries("Aqua Pilgrim Series", avatar, channel);
            }

            //Unlock Crusader
            if (avatar.Dungeons.Count >= 6)
            {
                _ = GoldenSunCommands.AwardClassSeries("Crusader Series", avatar, channel);
            }

            if (avatar.ServerStats.DungeonsCompleted >= 12)
            {
                _ = GoldenSunCommands.AwardClassSeries("Air Pilgrim Series", avatar, channel);

            }
            string csvline = $"{DateTime.Now:s},Dungeon,{dungeon.Name},{avatar.Name}{Environment.NewLine}";
            File.AppendAllText(BattleFile, csvline);

            await Task.CompletedTask;
        }

        internal static async Task UserWonSingleBattle(UserAccount avatar, BattleDifficulty difficulty)
        {
            string csvline = $"{DateTime.Now:s},Single,{difficulty},{avatar.Name}{Environment.NewLine}";
            File.AppendAllText(BattleFile, csvline);
            await Task.CompletedTask;
        }

        internal static async Task UserSentCommand(SocketUser user, IMessageChannel channel)
        {
            var userAccount = EntityConverter.ConvertUser(user);
            userAccount.ServerStats.CommandsUsed++;
            UserAccountProvider.StoreUser(userAccount);
            if (userAccount.ServerStats.CommandsUsed >= 100)
            {
                await GoldenSunCommands.AwardClassSeries("Scrapper Series", user, channel);
            }
        }

        internal static async Task UserWonRPS(SocketGuildUser user, SocketTextChannel channel)
        {
            var userAccount = EntityConverter.ConvertUser(user);
            userAccount.ServerStats.RpsWins++;
            userAccount.ServerStats.RpsStreak++;
            UserAccountProvider.StoreUser(userAccount);

            if (userAccount.ServerStats.RpsWins >= 3)
            {
                await GoldenSunCommands.AwardClassSeries("Aqua Seer Series", user, channel);
            }
        }

        internal static void UserDidNotWinRPS(SocketGuildUser user)
        {
            var userAccount = EntityConverter.ConvertUser(user);
            userAccount.ServerStats.RpsStreak = 0;
            UserAccountProvider.StoreUser(userAccount);
        }

        internal static async Task UserLostBattle(UserAccount userAccount, ITextChannel battleChannel)
        {
            uint oldLevel = userAccount.LevelNumber;
            userAccount.AddXp((uint)(new Random()).Next(1, 10));
            uint newLevel = userAccount.LevelNumber;

            userAccount.ServerStats.ColossoStreak = 0;

            UserAccountProvider.StoreUser(userAccount);
            if (oldLevel != newLevel)
            {
                var user = (SocketGuildUser)await battleChannel.GetUserAsync(userAccount.ID); // Where(s => s. == userAccount.ID).First();
                Leveling.LevelUp(userAccount, user, (SocketTextChannel)battleChannel);
            }

            await Task.CompletedTask;
        }

        internal static async Task UserLookedUpPsynergy(SocketGuildUser user, SocketTextChannel channel)
        {
            var userAccount = EntityConverter.ConvertUser(user);
            userAccount.ServerStats.LookedUpInformation++;
            UserAccountProvider.StoreUser(userAccount);

            if (userAccount.ServerStats.LookedUpInformation >= 21)
            {
                _ = GoldenSunCommands.AwardClassSeries("Apprentice Series", user, channel);
            }
            await Task.CompletedTask;
        }

        internal static async Task UserLookedUpClass(SocketGuildUser user, SocketTextChannel channel)
        {
            var userAccount = EntityConverter.ConvertUser(user);
            userAccount.ServerStats.LookedUpClass++;
            UserAccountProvider.StoreUser(userAccount);

            if (userAccount.ServerStats.LookedUpClass >= 11)
            {
                _ = GoldenSunCommands.AwardClassSeries("Page Series", user, channel);
            }
            await Task.CompletedTask;
        }
    }
}