﻿using System;
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
        internal static string BattleFile = $"Logs/Battles_{Global.DateString}.log";
        internal static async void UserWonColosso(SocketGuildUser user, IMessageChannel channel)
        {
            var userAccount = UserAccounts.GetAccount(user);
            uint oldLevel = userAccount.LevelNumber;
            userAccount.AddXp((uint)(new Random()).Next(40, 70));
            uint newLevel = userAccount.LevelNumber;


            userAccount.ServerStats.ColossoWins++;
            userAccount.ServerStats.ColossoStreak++;
            if (userAccount.ServerStats.ColossoStreak > userAccount.ServerStats.ColossoHighestStreak)
            {
                userAccount.ServerStats.ColossoHighestStreak = userAccount.ServerStats.ColossoStreak;
            }

            UserAccounts.SaveAccounts();
            if (oldLevel != newLevel)
            {
                Leveling.LevelUp(userAccount, user, channel);
            }
            if (userAccount.ServerStats.ColossoWins >= 15)
            {
                await GoldenSun.AwardClassSeries("Brute Series", user, channel);
            }

            await Task.CompletedTask;
        }

        internal static async void UserLostColosso(SocketGuildUser user, IMessageChannel channel)
        {
            var userAccount = UserAccounts.GetAccount(user);
            uint oldLevel = userAccount.LevelNumber;
            userAccount.AddXp((uint)(new Random()).Next(1, 10));
            uint newLevel = userAccount.LevelNumber;
            userAccount.ServerStats.ColossoStreak = 0;
            UserAccounts.SaveAccounts();

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

            UserAccounts.SaveAccounts();
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
                await GoldenSun.AwardClassSeries("Brute Series", userAccount, channel);
            }

            if (bs.KillsByHand >= 210)
            {
                await GoldenSun.AwardClassSeries("Samurai Series", userAccount, channel);
            }

            if (bs.DamageDealt >= 666666)
            {
                await GoldenSun.AwardClassSeries("Ninja Series", userAccount, channel);
            }

            if (bs.SoloBattles >= 100)
            {
                await GoldenSun.AwardClassSeries("Ranger Series", userAccount, channel);
            }

            if (bs.TotalTeamMates >= 100)
            {
                await GoldenSun.AwardClassSeries("Dragoon Series", userAccount, channel);
            }

            if (bs.HPhealed >= 333333)
            {
                await GoldenSun.AwardClassSeries("White Mage Series", userAccount, channel);
            }

            if (bs.Revives >= 25)
            {
                await GoldenSun.AwardClassSeries("Medium Series", userAccount, channel);
            }
        }

        internal static async Task UserWonPvP(UserAccount avatar, ITextChannel lobbyChannel, int numberOfWinners, int numberOfLosers)
        {
            _ = GoldenSun.AwardClassSeries("Swordsman Series", avatar, lobbyChannel);
            string csvline = $"{DateTime.Now.TimeOfDay},PvP,{numberOfWinners}vs{numberOfLosers},{avatar.Name}{Environment.NewLine}";
            File.AppendAllText(BattleFile, csvline);
            await Task.CompletedTask;
        }

        internal static async Task UserWonEndless(UserAccount avatar, ITextChannel lobbyChannel, int winsInARow, EndlessMode mode, int nOfPlayers, string TeamMatesNames)
        {
            if(mode == EndlessMode.Default)
            {
                avatar.ServerStats.EndlessStreak.AddStreak(winsInARow, nOfPlayers, TeamMatesNames);
            } else
            {
                avatar.ServerStats.LegacyStreak.AddStreak(winsInARow, nOfPlayers, TeamMatesNames);
            }

            await Task.CompletedTask;
        }

        internal static async Task UserFinishedEndless(UserAccount avatar, ITextChannel lobbyChannel, int winsInARow, EndlessMode mode)
        {
            string csvline = $"{DateTime.Now.TimeOfDay},Endless {mode},{winsInARow},{avatar.Name}{Environment.NewLine}";
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
                    _ = GoldenSun.AwardClassSeries("Hermit Series", avatar, channel);
                }
            }
            avatar.ServerStats.LastDungeon = dungeon.Name;
            if (dungeon.Name == "Mercury Lighthouse")
            {
                _ = GoldenSun.AwardClassSeries("Aqua Pilgrim Series", avatar, channel);
            }

            //Unlock Crusader
            if (avatar.Dungeons.Count >= 6)
            {
                _ = GoldenSun.AwardClassSeries("Crusader Series", avatar, channel);
            }

            if (avatar.ServerStats.DungeonsCompleted >= 12)
            {
                _ = GoldenSun.AwardClassSeries("Air Pilgrim Series", avatar, channel);

            }
            string csvline = $"{DateTime.Now.TimeOfDay},Dungeon,{dungeon.Name},{avatar.Name}{Environment.NewLine}";
            File.AppendAllText(BattleFile, csvline);

            await Task.CompletedTask;
        }

        internal static async Task UserWonSingleBattle(UserAccount avatar, ITextChannel lobbyChannel, BattleDifficulty difficulty)
        {
            string csvline = $"{DateTime.Now.TimeOfDay},Single,{difficulty},{avatar.Name}{Environment.NewLine}";
            File.AppendAllText(BattleFile, csvline);
            await Task.CompletedTask;
        }

        internal static async Task UserSentCommand(SocketUser user, IMessageChannel channel)
        {
            var userAccount = UserAccounts.GetAccount(user);
            userAccount.ServerStats.CommandsUsed++;
            if (userAccount.ServerStats.CommandsUsed >= 100)
            {
                await GoldenSun.AwardClassSeries("Scrapper Series", user, channel);
            }
        }

        internal static async Task UserWonRPS(SocketGuildUser user, SocketTextChannel channel)
        {
            var userAccount = UserAccounts.GetAccount(user);
            userAccount.ServerStats.RpsWins++;
            userAccount.ServerStats.RpsStreak++;
            UserAccounts.SaveAccounts();

            if (userAccount.ServerStats.RpsWins == 3)
            {
                await GoldenSun.AwardClassSeries("Aqua Seer Series", user, channel);
            }
        }

        internal static void UserDidNotWinRPS(SocketGuildUser user)
        {
            var userAccount = UserAccounts.GetAccount(user);
            userAccount.ServerStats.RpsStreak = 0;
            UserAccounts.SaveAccounts();
        }

        internal static async Task UserLostBattle(UserAccount userAccount, ITextChannel battleChannel)
        {
            uint oldLevel = userAccount.LevelNumber;
            userAccount.AddXp((uint)(new Random()).Next(1, 10));
            uint newLevel = userAccount.LevelNumber;

            userAccount.ServerStats.ColossoStreak = 0;

            UserAccounts.SaveAccounts();
            if (oldLevel != newLevel)
            {
                var user = (SocketGuildUser)await battleChannel.GetUserAsync(userAccount.ID); // Where(s => s. == userAccount.ID).First();
                Leveling.LevelUp(userAccount, user, (SocketTextChannel)battleChannel);
            }

            await Task.CompletedTask;
        }

        internal static async Task UserLookedUpPsynergy(SocketGuildUser user, SocketTextChannel channel)
        {
            var userAccount = UserAccounts.GetAccount(user);
            userAccount.ServerStats.LookedUpInformation++;
            UserAccounts.SaveAccounts();

            if (userAccount.ServerStats.LookedUpInformation >= 21)
            {
                await GoldenSun.AwardClassSeries("Apprentice Series", user, channel);
            }
        }

        internal static async Task UserLookedUpClass(SocketGuildUser user, SocketTextChannel channel)
        {
            var userAccount = UserAccounts.GetAccount(user);
            userAccount.ServerStats.LookedUpClass++;
            UserAccounts.SaveAccounts();

            if (userAccount.ServerStats.LookedUpClass >= 21)
            {
                await GoldenSun.AwardClassSeries("Page Series", user, channel);
            }
        }
    }
}