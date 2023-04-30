using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using IodemBot.Core.UserManagement;
using IodemBot.Modules;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.Core.Leveling
{
    internal static class Leveling
    {
        internal static ulong[] BlackListedChannels =
        {
            358276942337671178, 535082629091950602, 536721357216677891, 536721375323357196, 536721392620535830,
            535199363907977226, 565910418741133315
        };

        internal static async void UserSentMessage(SocketGuildUser user, SocketTextChannel channel)
        {
            if (channel == null || BlackListedChannels.Contains(channel.Id) || user == null) return;

            if (channel.Id == GuildSettings.GetGuildSettings(channel.Guild).ColossoChannel?.Id) return;

            var userAccount = EntityConverter.ConvertUser(user);

            // if the user has a timeout, ignore them
            var sinceLastXp = DateTime.UtcNow - userAccount.LastXp;
            var oldLevel = userAccount.LevelNumber;

            if (sinceLastXp.TotalMinutes >= 3)
            {
                userAccount.LastXp = DateTime.UtcNow;
                userAccount.AddXp((uint)Global.RandomNumber(30, 50));
            }

            if (DateTime.Now.Date != userAccount.ServerStats.LastDayActive.Date)
            {
                userAccount.ServerStats.UniqueDaysActive++;
                userAccount.ServerStats.LastDayActive = DateTime.Now.Date;
            }

            if (channel.Id == GuildSettings.GetGuildSettings(channel.Guild)?.ColossoChannel?.Id)
            {
                userAccount.ServerStats.MessagesInColossoTalks++;
                if (userAccount.ServerStats.MessagesInColossoTalks >= 50)
                    _ = GoldenSunCommands.AwardClassSeries("Swordsman Series", userAccount, channel);
            }

            var newLevel = userAccount.LevelNumber;

            if (oldLevel != newLevel) LevelUp(userAccount, user, channel);

            if (channel.Id != userAccount.ServerStats.MostRecentChannel)
            {
                userAccount.ServerStats.MostRecentChannel = channel.Id;
                userAccount.ServerStats.ChannelSwitches += 2;
                if (userAccount.ServerStats.ChannelSwitches >= 14)
                    _ = GoldenSunCommands.AwardClassSeries("Air Pilgrim Series", userAccount, channel);
            }
            else
            {
                if (userAccount.ServerStats.ChannelSwitches > 0) userAccount.ServerStats.ChannelSwitches--;
            }
            UserAccountProvider.StoreUser(userAccount);
            await Task.CompletedTask;
        }

        internal static async void LevelUp(UserAccount userAccount, SocketGuildUser user,
            IMessageChannel channel = null)
        {
            if (channel == null || userAccount == null || user == null)
            {
                Console.WriteLine($"userAccount: {channel}, user: {user}, channel: {channel}");
                return;
            }

            if (userAccount.LevelNumber < 10 && userAccount.LevelNumber % 5 > 0)
                channel = GuildSettings.GetGuildSettings(user.Guild).CommandChannel;

            // the user leveled up
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get(userAccount.Element.ToString()));
            embed.WithTitle("LEVEL UP!");
            embed.WithDescription("<:Up_Arrow:571309108289077258> " + userAccount.GsClass + " " + user.Mention +
                                  " just leveled up!");
            embed.AddField("LEVEL", userAccount.LevelNumber, true);
            embed.AddField("XP", $"{userAccount.Xp}{(userAccount.Oaths.IsOathActive(Oath.Oaf) ? $" (effective: {(ulong)(userAccount.Xp / 4 / userAccount.XpBoost)})" : "")}", true);
            if (userAccount.LevelNumber == 10)
                embed.AddField("Congratulations!", "You have unlocked Easy mode in the Weyard battle channels!");
            else if (userAccount.LevelNumber == 30)
                embed.AddField("Congratulations!", "You have unlocked Medium mode in the Weyard battle channels!");
            else if (userAccount.LevelNumber == 50)
                embed.AddField("Congratulations!",
                    "You have unlocked Hard mode in the Weyard battle channels, as well as the Endless mode!");
            _ = channel.SendMessageAsync("", embed: embed.Build());
            await Task.CompletedTask;
        }
    }
}