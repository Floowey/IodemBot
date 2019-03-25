using Discord;
using Discord.WebSocket;
using IodemBot.Core.UserManagement;
using IodemBot.Modules;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IodemBot.Core.Leveling
{
    internal static class Leveling
    {
        internal static ulong[] blackListedChannels = new ulong[] { 358276942337671178 };
        public static int rate = 200;
        public static int cutoff = 125000;

        internal static async void UserSentMessage(SocketGuildUser user, SocketTextChannel channel)
        {
            if (blackListedChannels.Contains(channel.Id))
            {
                return;
            }

            var userAccount = UserAccounts.GetAccount(user);

            // if the user has a timeout, ignore them
            var sinceLastXP = DateTime.UtcNow - userAccount.lastXP;
            if (sinceLastXP.Minutes < 2)
            {
                return;
            }

            userAccount.lastXP = DateTime.UtcNow;

            uint oldLevel = userAccount.LevelNumber;
            userAccount.XP += (uint)(new Random()).Next(30, 60);

            if ((DateTime.Now.Date != userAccount.lastDayActive.Date))
            {
                userAccount.uniqueDaysActive++;
                userAccount.lastDayActive = DateTime.Now.Date;
            }

            if ((DateTime.Now - user.JoinedAt).Value.TotalDays > 30)
            {
                await GoldenSun.AwardClassSeries("Hermit Series", user, channel);
            }

            if (channel.Id != userAccount.ServerStats.mostRecentChannel)
            {
                userAccount.ServerStats.mostRecentChannel = channel.Id;
                userAccount.ServerStats.channelSwitches += 2;
                if (userAccount.ServerStats.channelSwitches >= 10)
                {
                    await GoldenSun.AwardClassSeries("Air Pilgrim Series", user, channel);
                }
            }
            else
            {
                if (userAccount.ServerStats.channelSwitches >= 1)
                {
                    userAccount.ServerStats.channelSwitches--;
                }
            }

            UserAccounts.SaveAccounts();
            uint newLevel = userAccount.LevelNumber;

            if (oldLevel != newLevel)
            {
                LevelUp(userAccount, user, channel);
            }

            await Task.CompletedTask;
        }

        internal static async void LevelUp(UserAccount userAccount, SocketGuildUser user, SocketTextChannel channel = null)
        {
            if (userAccount.LevelNumber < 10 && (userAccount.LevelNumber % 5) > 0)
            {
                channel = (SocketTextChannel)user.Guild.Channels.Where(c => c.Id == 358276942337671178).FirstOrDefault();
            }
            if (channel == null)
            {
                return;
            }
            // the user leveled up
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.get(userAccount.element.ToString()));
            embed.WithTitle("LEVEL UP!");
            embed.WithDescription(userAccount.gsClass + " " + user.Mention ?? "@" + userAccount.Name + " just leveled up!");
            embed.AddField("LEVEL", userAccount.LevelNumber, true);
            embed.AddField("XP", userAccount.XP, true);
            await channel.SendMessageAsync("", embed: embed.Build());
        }

        internal static async void UserAddedReaction(SocketGuildUser user, SocketTextChannel channel)
        {
            await Task.CompletedTask;
        }

        internal static async void UserSentFile(SocketGuildUser user, SocketTextChannel channel)
        {
            await Task.CompletedTask;
        }

        internal static uint XPforNextLevel(uint xp)
        {
            uint curLevel;
            uint xpneeded;
            if (xp <= cutoff)
            {
                curLevel = (uint)Math.Sqrt(xp / 50);
                xpneeded = (uint)Math.Pow((curLevel + 1), 2) * 50 - xp;
            }
            else
            {
                curLevel = (uint)(50 - Math.Sqrt(cutoff / rate) + Math.Sqrt(xp / rate));
                xpneeded = (uint)Math.Pow((curLevel + 1) - 25, 2) * 200 - xp;
            }

            return xpneeded;
        }
    }
}