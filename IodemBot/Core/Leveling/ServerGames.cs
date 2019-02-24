using Discord;
using Discord.Rest;
using Discord.WebSocket;
using IodemBot.Core.UserManagement;
using IodemBot.Modules;
using IodemBot.Modules.ColossoBattles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Core.Leveling
{
    internal static class ServerGames
    {
        internal static async void UserWonColosso(SocketGuildUser user, SocketTextChannel channel)
        {
            var userAccount = UserAccounts.GetAccount(user);
            uint oldLevel = userAccount.LevelNumber;
            userAccount.XP += (uint)(new Random()).Next(40, 70);
            uint newLevel = userAccount.LevelNumber;

            userAccount.ColossoWins++;
            userAccount.ColossoStreak++;
            if (userAccount.ColossoStreak > userAccount.ColossoHighestStreak) userAccount.ColossoHighestStreak = userAccount.ColossoStreak;

            UserAccounts.SaveAccounts();
            if (oldLevel != newLevel)
            {
                Leveling.LevelUp(userAccount, user, channel);
            }
            if (userAccount.ColossoWins >= 15) await GoldenSun.AwardClassSeries("Brute Series", user, channel);
            await Task.CompletedTask;
        }

        internal static async void UserLostColosso(SocketGuildUser user, SocketTextChannel channel)
        {
            var userAccount = UserAccounts.GetAccount(user);
            uint oldLevel = userAccount.LevelNumber;
            userAccount.XP += (uint)(new Random()).Next(1, 10);
            uint newLevel = userAccount.LevelNumber;
            userAccount.ColossoStreak = 0;
            UserAccounts.SaveAccounts();

            if (oldLevel != newLevel)
            {
                Leveling.LevelUp(userAccount, user, channel);
            }
            await Task.CompletedTask;
        }

        internal static async Task UserSentCommand(SocketGuildUser user, SocketTextChannel channel)
        {
            var userAccount = UserAccounts.GetAccount(user);
            userAccount.commandsUsed++;
            if (userAccount.commandsUsed >= 100)
            {
                await GoldenSun.AwardClassSeries("Scrapper Series", user, channel);
            }
        }

        internal static async Task UserWonRPS(SocketGuildUser user, SocketTextChannel channel)
        {
            var userAccount = UserAccounts.GetAccount(user);
            userAccount.rpsWins++;
            userAccount.rpsStreak++;
            UserAccounts.SaveAccounts();

            if(userAccount.rpsStreak == 4)
            {
                await GoldenSun.AwardClassSeries("Air Seer Series", user, channel);
            }

            if(userAccount.rpsWins == 15)
            {
                await GoldenSun.AwardClassSeries("Aqua Seer Series", user, channel);
            }
        }

        internal static void UserDidNotWinRPS(SocketGuildUser user)
        {
            var userAccount = UserAccounts.GetAccount(user);
            userAccount.rpsStreak = 0;
            UserAccounts.SaveAccounts();
        }

        internal static async Task UserWonBattle(UserAccount userAccount, ColossoPvE.BattleDifficulty diff, ITextChannel battleChannel)
        {
            uint oldLevel = userAccount.LevelNumber;
            userAccount.XP += (uint)(new Random()).Next(10, 20)*(uint) Math.Pow(((int) diff +1),2) * 2;
            uint newLevel = userAccount.LevelNumber;

            userAccount.ColossoWins++;
            userAccount.ColossoStreak++;
            if (userAccount.ColossoStreak > userAccount.ColossoHighestStreak) userAccount.ColossoHighestStreak = userAccount.ColossoStreak;

            if (userAccount.ColossoWins >= 15) await GoldenSun.AwardClassSeries("Brute Series", userAccount, (SocketTextChannel)battleChannel);

            if (userAccount.killsByHand >= 161) await GoldenSun.AwardClassSeries("Samurai Series", userAccount, (SocketTextChannel)battleChannel);
            if (userAccount.damageDealt >= 666666) await GoldenSun.AwardClassSeries("Ninja Series", userAccount, (SocketTextChannel)battleChannel);
            if (userAccount.soloBattles >= 50) await GoldenSun.AwardClassSeries("Ranger Series", userAccount, (SocketTextChannel)battleChannel);
            if (userAccount.totalTeamMates >= 100) await GoldenSun.AwardClassSeries("Dragoon Series", userAccount, (SocketTextChannel)battleChannel);
            if (userAccount.HPhealed >= 333333) await GoldenSun.AwardClassSeries("White Mage Series", userAccount, (SocketTextChannel) battleChannel);
            if (userAccount.revives >= 50) await GoldenSun.AwardClassSeries("Medium Series", userAccount, (SocketTextChannel)battleChannel);

            UserAccounts.SaveAccounts();
            if (oldLevel != newLevel)
            {
                var user = (SocketGuildUser) await battleChannel.GetUserAsync(userAccount.ID); // Where(s => s. == userAccount.ID).First();
                Leveling.LevelUp(userAccount, user, (SocketTextChannel) battleChannel);
            }

            await Task.CompletedTask;
        }

        internal static async Task UserHasCursed(SocketGuildUser user, SocketTextChannel channel)
        {
            var userAccount = UserAccounts.GetAccount(user);
            if(userAccount.hasQuotedMatthew && userAccount.hasWrittenCurse)
            {
                await GoldenSun.AwardClassSeries("Curse Mage Series", user, channel);
            }
        }

        internal static async Task UserLostBattle(UserAccount userAccount, ColossoPvE.BattleDifficulty diff, ITextChannel battleChannel)
        {
            uint oldLevel = userAccount.LevelNumber;
            userAccount.XP += (uint)(new Random()).Next(0, 10);
            uint newLevel = userAccount.LevelNumber;

            userAccount.ColossoStreak = 0;

            UserAccounts.SaveAccounts();
            if (oldLevel != newLevel)
            {
                var user = (SocketGuildUser)await battleChannel.GetUserAsync(userAccount.ID); // Where(s => s. == userAccount.ID).First();
                Leveling.LevelUp(userAccount, user, (SocketTextChannel)battleChannel);
            }

            await Task.CompletedTask;
        }
    }
}
