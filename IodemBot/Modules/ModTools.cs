using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static IodemBot.Modules.GoldenSunMechanics.Psynergy;

namespace IodemBot.Modules
{
    public class ModTools : ModuleBase<SocketCommandContext>
    {
        [Command("Ban")]
        [RequireUserPermission(GuildPermission.BanMembers)]
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
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireBotPermission(GuildPermission.KickMembers)]
        public async Task KickUser(IGuildUser user, string reason = "No reason provided.")
        {
            await user.KickAsync(reason);
        }
        [Command("setupIodem")]
        [Remarks("One Time Use only, if it works")]
        [RequireOwner]
        public async Task setupIodem()
        {
            foreach (SocketGuildUser user in Context.Guild.Users)
            {
                var account = UserAccounts.GetAccount(user);

                if (user.Roles.Where(r => r.Id == 497198579207897088).FirstOrDefault() != null)
                {//Venus
                    account.element = Element.Venus;
                }
                else if (user.Roles.Where(r => r.Id == 497198845994991646).FirstOrDefault() != null)
                {//Mars
                    account.element = Element.Mars;
                }
                else if (user.Roles.Where(r => r.Id == 497199050408460288).FirstOrDefault() != null)
                {//Jupiter
                    account.element = Element.Jupiter;
                }
                else if (user.Roles.Where(r => r.Id == 497212342896033812).FirstOrDefault() != null)
                {//Mercury
                    account.element = Element.Mercury;
                }
                else
                {
                    account.element = Element.none;
                }

                account.Name = user.Username;
                Console.WriteLine($"{account.Name} is a {account.element} Adept");
            }
            UserAccounts.SaveAccounts();
        }

        [Command("Emotes")]
        public async Task Emotes()
        {
            var s = string.Join("\n", Context.Guild.Emotes.Select(e => $"{e.ToString()} \\<:{e.Name}:{e.Id}>"));
            if (s.Length > 2000)
            {
                await Context.Channel.SendMessageAsync(s.Substring(0,2000));
                await Context.Channel.SendMessageAsync(s.Substring(2000));
            } else
            {
                await Context.Channel.SendMessageAsync(s);
            }
            
        }
    }
}