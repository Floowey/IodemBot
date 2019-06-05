using Discord;
using Discord.WebSocket;
using IodemBot.Core.UserManagement;
using IodemBot.Modules;
using IodemBot.Modules.ColossoBattles;
using IodemBot.Modules.GoldenSunMechanics;
using System;
using System.Linq;
using System.Threading.Tasks;
using static IodemBot.Modules.ColossoBattles.ColossoPvE;

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

        internal static async void UserLostColosso(SocketGuildUser user, SocketTextChannel channel)
        {
            var userAccount = UserAccounts.GetAccount(user);
            uint oldLevel = userAccount.LevelNumber;
            userAccount.XP += (uint)(new Random()).Next(1, 10);
            uint newLevel = userAccount.LevelNumber;
            userAccount.ServerStats.ColossoStreak = 0;
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

            if (userAccount.ServerStats.RpsStreak == 4)
            {
                await GoldenSun.AwardClassSeries("Air Seer Series", user, channel);
            }

            if (userAccount.ServerStats.RpsWins == 15)
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

        internal static async Task UserWonBattle(UserAccount userAccount, int winsInARow, int LureCaps, BattleStats battleStats, BattleDifficulty diff, ITextChannel battleChannel)
        {
            uint oldLevel = userAccount.LevelNumber;
            var xpawarded = (uint)(new Random()).Next(20 + 5 * LureCaps, 40 + 10 * LureCaps) * Math.Min(3, (uint)Math.Pow(((int)diff + 1), 2));
            userAccount.XP += xpawarded;
            userAccount.Inv.AddBalance(xpawarded / 2);
            uint newLevel = userAccount.LevelNumber;

            userAccount.ServerStats.ColossoWins++;
            userAccount.ServerStats.ColossoStreak++;
            userAccount.ServerStats.ColossoHighestStreak = Math.Max(userAccount.ServerStats.ColossoHighestStreak, userAccount.ServerStats.ColossoStreak);
            userAccount.ServerStats.ColossoHighestRoundEndless = Math.Max(userAccount.ServerStats.ColossoHighestRoundEndless, winsInARow);

            userAccount.BattleStats += battleStats;
            var bs = userAccount.BattleStats;

            if (Global.Random.Next(0, 100) <= 9 + battleStats.TotalTeamMates * 2 + 4 * LureCaps)
            {
                ChestQuality awardedChest = GetRandomChest(diff);
                userAccount.Inv.AwardChest(awardedChest);
                var embed = new EmbedBuilder();
                embed.WithColor(Colors.Get("Iodem"));
                embed.WithDescription($"{((SocketTextChannel)battleChannel).Users.Where(u => u.Id == userAccount.ID).FirstOrDefault().Mention} found a {Inventory.ChestIcons[awardedChest]} {awardedChest} Chest!");
                await battleChannel.SendMessageAsync("", false, embed.Build());
            }

            if (userAccount.ServerStats.ColossoWins >= 15)
            {
                await GoldenSun.AwardClassSeries("Brute Series", userAccount, (SocketTextChannel)battleChannel);
            }

            if (bs.KillsByHand >= 161)
            {
                await GoldenSun.AwardClassSeries("Samurai Series", userAccount, (SocketTextChannel)battleChannel);
            }

            if (bs.DamageDealt >= 666666)
            {
                await GoldenSun.AwardClassSeries("Ninja Series", userAccount, (SocketTextChannel)battleChannel);
            }

            if (bs.SoloBattles >= 50)
            {
                await GoldenSun.AwardClassSeries("Ranger Series", userAccount, (SocketTextChannel)battleChannel);
            }

            if (bs.TotalTeamMates >= 100)
            {
                await GoldenSun.AwardClassSeries("Dragoon Series", userAccount, (SocketTextChannel)battleChannel);
            }

            if (bs.HPhealed >= 333333)
            {
                await GoldenSun.AwardClassSeries("White Mage Series", userAccount, (SocketTextChannel)battleChannel);
            }

            if (bs.Revives >= 50)
            {
                await GoldenSun.AwardClassSeries("Medium Series", userAccount, (SocketTextChannel)battleChannel);
            }

            UserAccounts.SaveAccounts();
            if (oldLevel != newLevel)
            {
                var user = (SocketGuildUser)await battleChannel.GetUserAsync(userAccount.ID); // Where(s => s. == userAccount.ID).First();
                Leveling.LevelUp(userAccount, user, (SocketTextChannel)battleChannel);
            }

            await Task.CompletedTask;
        }

        private static ChestQuality GetRandomChest(BattleDifficulty diff)
        {
            ChestQuality[] chests;
            switch (diff)
            {
                case BattleDifficulty.Tutorial:
                    chests = new ChestQuality[] { ChestQuality.Wooden };
                    return chests[Global.Random.Next(0, chests.Length)];

                case BattleDifficulty.Easy:
                default:
                    chests = new ChestQuality[] { ChestQuality.Wooden, ChestQuality.Wooden, ChestQuality.Normal, ChestQuality.Normal, ChestQuality.Normal, ChestQuality.Normal, ChestQuality.Normal, ChestQuality.Silver };
                    return chests[Global.Random.Next(0, chests.Length)];

                case BattleDifficulty.Medium:
                    chests = new ChestQuality[] { ChestQuality.Wooden, ChestQuality.Normal, ChestQuality.Normal, ChestQuality.Silver, ChestQuality.Silver, ChestQuality.Silver };
                    return chests[Global.Random.Next(0, chests.Length)];

                case BattleDifficulty.MediumRare:
                    chests = new ChestQuality[] { ChestQuality.Normal, ChestQuality.Silver, ChestQuality.Silver, ChestQuality.Silver, ChestQuality.Gold };
                    return chests[Global.Random.Next(0, chests.Length)];

                case BattleDifficulty.Hard:
                    chests = new ChestQuality[] { ChestQuality.Silver, ChestQuality.Gold, ChestQuality.Gold, ChestQuality.Gold, ChestQuality.Gold, ChestQuality.Adept };
                    return chests[Global.Random.Next(0, chests.Length)];
            }
        }

        internal static async Task UserHasCursed(SocketGuildUser user, SocketTextChannel channel)
        {
            var userAccount = UserAccounts.GetAccount(user);
            if (userAccount.ServerStats.HasQuotedMatthew && userAccount.ServerStats.HasWrittenCurse)
            {
                await GoldenSun.AwardClassSeries("Curse Mage Series", user, channel);
            }
        }

        internal static async Task UserLostBattle(UserAccount userAccount, ColossoPvE.BattleDifficulty diff, ITextChannel battleChannel)
        {
            uint oldLevel = userAccount.LevelNumber;
            userAccount.XP += (uint)(new Random()).Next(0, 10);
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