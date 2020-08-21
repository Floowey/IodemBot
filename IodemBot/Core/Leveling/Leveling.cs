using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using IodemBot.Core.UserManagement;
using IodemBot.Discord;
using IodemBot.Modules;

namespace IodemBot.Core.Leveling
{
    internal static class Leveling
    {
        internal static ulong[] blackListedChannels = new ulong[] { 358276942337671178, 535082629091950602, 536721357216677891, 536721375323357196, 536721392620535830, 535199363907977226, 565910418741133315 };
        public static int rate = 200;
        public static int cutoff = 125000;

        internal static async void UserSentMessage(SocketGuildUser user, SocketTextChannel channel)
        {
            if (blackListedChannels.Contains(channel.Id) || channel == null || user == null)
            {
                return;
            }

            if (channel.Id == GuildSettings.GetGuildSettings(channel.Guild).ColossoChannel?.Id)
            {
                return;
            }

            var userAccount = EntityConverter.ConvertUser(user);

            // if the user has a timeout, ignore them
            var sinceLastXP = DateTime.UtcNow - userAccount.LastXP;
            uint oldLevel = userAccount.LevelNumber;

            if (sinceLastXP.TotalMinutes >= 3)
            {
                userAccount.LastXP = DateTime.UtcNow;
                userAccount.AddXp((uint)(new Random()).Next(30, 50));
            }

            if (DateTime.Now.Date != userAccount.ServerStats.LastDayActive.Date)
            {
                userAccount.ServerStats.UniqueDaysActive++;
                userAccount.ServerStats.LastDayActive = DateTime.Now.Date;

                //if ((DateTime.Now - user.JoinedAt).Value.TotalDays > 30)
                //{
                //    await GoldenSun.AwardClassSeries("Hermit Series", user, channel);
                //}
            }

            if (channel.Id != userAccount.ServerStats.MostRecentChannel)
            {
                userAccount.ServerStats.MostRecentChannel = channel.Id;
                userAccount.ServerStats.ChannelSwitches += 2;
                if (userAccount.ServerStats.ChannelSwitches >= 14)
                {
                    await GoldenSun.AwardClassSeries("Air Pilgrim Series", user, channel);
                }
            }
            else
            {
                if (userAccount.ServerStats.ChannelSwitches > 0)
                {
                    userAccount.ServerStats.ChannelSwitches--;
                }
            }

            if (channel.Id == GuildSettings.GetGuildSettings(channel.Guild)?.ColossoChannel?.Id)
            {
                userAccount.ServerStats.MessagesInColossoTalks++;
                if (userAccount.ServerStats.MessagesInColossoTalks >= 50)
                {
                    await GoldenSun.AwardClassSeries("Swordsman Series", user, channel);
                }
            }

            UserAccountProvider.StoreUser(userAccount);
            uint newLevel = userAccount.LevelNumber;

            if (oldLevel != newLevel)
            {
                LevelUp(userAccount, user, channel);
            }

            await Task.CompletedTask;
        }

        internal static async void LevelUp(UserAccount userAccount, SocketGuildUser user, IMessageChannel channel = null)
        {
            if (userAccount.LevelNumber < 10 && (userAccount.LevelNumber % 5) > 0)
            {
                channel = GuildSettings.GetGuildSettings(user.Guild).CommandChannel;
            }
            if (channel == null)
            {
                return;
            }
            // the user leveled up
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get(userAccount.Element.ToString()));
            embed.WithTitle("LEVEL UP!");
            embed.WithDescription("<:Up_Arrow:571309108289077258> " + userAccount.GsClass + " " + user.Mention + " just leveled up!");
            embed.AddField("LEVEL", userAccount.LevelNumber, true);
            embed.AddField("XP", userAccount.XP, true);
            if (userAccount.LevelNumber == 10)
            {
                embed.AddField("Congratulations!", "You have unlocked Easy mode in the Weyard battle channels!");
            }
            else if (userAccount.LevelNumber == 30)
            {
                embed.AddField("Congratulations!", "You have unlocked Medium mode in the Weyard battle channels!");
            }
            else if (userAccount.LevelNumber == 50)
            {
                embed.AddField("Congratulations!", "You have unlocked Hard mode in the Weyard battle channels, as well as the Endless mode!");
            }
            await channel.SendMessageAsync("", embed: embed.Build());
        }

        internal static async void UserAddedReaction(SocketGuildUser user, SocketReaction reaction)
        {
            if (blackListedChannels.Contains(reaction.Channel.Id) || Modules.ColossoBattles.ColossoPvE.ChannelIds.Contains(reaction.Channel.Id))
            {
                return;
            }

            if (!Global.Client.Guilds.Any(g => g.Emotes.Any(e => e.Name == reaction.Emote.Name)))
            {
                return;
            }

            var userAccount = EntityConverter.ConvertUser(user);
            if (reaction.MessageId == userAccount.ServerStats.MostRecentChannel)
            {
                userAccount.ServerStats.ReactionsAdded++;
            }
            else
            {
                userAccount.ServerStats.ReactionsAdded += 5;
                userAccount.ServerStats.MostRecentChannel = reaction.MessageId;
            }
            await Task.CompletedTask;
            UserAccountProvider.StoreUser(userAccount);
        }
    }
}