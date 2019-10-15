using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IodemBot.Modules
{
    public class ModTools : ModuleBase<SocketCommandContext>
    {
        [Command("Ban")]
        [RequireModerator]
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task BanUser(IGuildUser user, string reason = "No reason provided.")
        {
            await user.Guild.AddBanAsync(user, 5, reason);
        }

        [Command("purge")]
        [Remarks("Purges A User's Last Messages. Default Amount To Purge Is 100")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task Clear(SocketGuildUser user, int amountOfMessagesToDelete = 100)
        {
            if (user == Context.User)
            {
                amountOfMessagesToDelete++; //Because it will count the purge command as a message
            }

            var messages = await Context.Message.Channel.GetMessagesAsync(amountOfMessagesToDelete + 1).FlattenAsync();

            var result = messages.Where(x => x.Author.Id == user.Id && x.CreatedAt >= DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(14)));

            await (Context.Message.Channel as SocketTextChannel).DeleteMessagesAsync(result);
        }

        [Command("purge")]
        [Remarks("Purges A User's Last Messages. Default Amount To Purge Is 100")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task Clear(int amountOfMessagesToDelete = 2)
        {
            var messages = await Context.Message.Channel.GetMessagesAsync(amountOfMessagesToDelete + 1).FlattenAsync();
            var result = messages.Where(x => x.CreatedAt >= DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(14)));

            await (Context.Message.Channel as SocketTextChannel).DeleteMessagesAsync(messages);
        }

        [Command("Kick")]
        [RequireModerator]
        public async Task KickUser(IGuildUser user, string reason = "No reason provided.")
        {
            await user.KickAsync(reason);
        }

        [Command("setupIodem")]
        [Remarks("One Time Use only, if it works")]
        [RequireOwner]
        public async Task SetupIodem()
        {
            foreach (SocketGuildUser user in Context.Guild.Users)
            {
                var account = UserAccounts.GetAccount(user);

                account.Name = user.DisplayName();
                Console.WriteLine($"{account.Name} is a {account.Element} Adept");
            }
            UserAccounts.SaveAccounts();
            await Task.CompletedTask;
        }

        [Command("Emotes")]
        [RequireStaff]
        public async Task Emotes()
        {
            var s = string.Join("\n", Context.Guild.Emotes.Select(e => $"{e.ToString()} \\<{(e.Animated ? "a" : "")}:{e.Name}:{e.Id}>"));
            if (s.Length > 2000)
            {
                await Context.Channel.SendMessageAsync(s.Substring(0, 2000));
                await Context.Channel.SendMessageAsync(s.Substring(2000));
            }
            else
            {
                await Context.Channel.SendMessageAsync(s);
            }
        }
    }
}