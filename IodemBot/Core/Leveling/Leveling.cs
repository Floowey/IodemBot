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
        internal static ulong[] blackListedChannels = new ulong[] { 358276942337671178, 535082629091950602, 536721357216677891, 536721375323357196, 536721392620535830 };
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
            uint oldLevel = userAccount.LevelNumber;

            if (sinceLastXP.Minutes >= 2)
            {
                userAccount.lastXP = DateTime.UtcNow;
                userAccount.XP += (uint)(new Random()).Next(30, 60);
            }

            if ((DateTime.Now.Date != userAccount.ServerStats.lastDayActive.Date))
            {
                userAccount.ServerStats.uniqueDaysActive++;
                userAccount.ServerStats.lastDayActive = DateTime.Now.Date;

                if ((DateTime.Now - user.JoinedAt).Value.TotalDays > 30)
                {
                    await GoldenSun.AwardClassSeries("Hermit Series", user, channel);
                }
            }

            if (channel.Id != userAccount.ServerStats.mostRecentChannel)
            {
                userAccount.ServerStats.mostRecentChannel = channel.Id;
                userAccount.ServerStats.channelSwitches += 2;
                if (userAccount.ServerStats.channelSwitches >= 14)
                {
                    await GoldenSun.AwardClassSeries("Air Pilgrim Series", user, channel);
                }
            }
            else
            {
                if (userAccount.ServerStats.channelSwitches > 0)
                {
                    userAccount.ServerStats.channelSwitches--;
                }
            }

            if (channel.Id == 546760009741107216)
            {
                userAccount.ServerStats.MessagesInColossoTalks++;
                if (userAccount.ServerStats.MessagesInColossoTalks >= 50)
                {
                    await GoldenSun.AwardClassSeries("Swordsman Series", user, channel);
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

        internal static async void UserAddedReaction(SocketGuildUser user, SocketReaction reaction)
        {
            if (blackListedChannels.Contains(reaction.MessageId))
            {
                return;
            }

            if (!Global.Client.Guilds.Any(g => g.Emotes.Any(e => e.Name == reaction.Emote.Name)))
            {
                return;
            }

            var userAccount = UserAccounts.GetAccount(user);
            if(reaction.MessageId == userAccount.ServerStats.mostRecentChannel)
            {
                userAccount.ServerStats.ReactionsAdded++;
            } else
            {
                userAccount.ServerStats.ReactionsAdded += 5;
                userAccount.ServerStats.mostRecentChannel = reaction.MessageId;
            }

            if (userAccount.ServerStats.ReactionsAdded >= 50)
            {
                await GoldenSun.AwardClassSeries("Air Pilgrim Series", user, (SocketTextChannel) Global.Client.GetChannel(546760009741107216));
            }
            UserAccounts.SaveAccounts();
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