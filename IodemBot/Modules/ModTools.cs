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

        private Dictionary<ulong, userImage> aprilFoolsUsers = new Dictionary<ulong, userImage>();
        public struct userImage
        {
            public ulong id { get; set; }
            public string name { get; set; }
            public Element element { get; set; }
            public int classToggle { get; set; }

        }

        private SocketRole venusRole;
        private SocketRole marsRole;
        private SocketRole jupiterRole;
        private SocketRole mercuryRole;
        private SocketRole exathi;

        [Command("AprilFoolsOn")]
        public async Task aprilFoolsOn()
        {
            venusRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Venus Adepts");
            marsRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Mars Adepts");
            jupiterRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Jupiter Adepts");
            mercuryRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Mercury Adepts");
            exathi = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Exathi") ?? venusRole;

            Global.Client.UserJoined += Client_UserJoined;
            foreach (SocketGuildUser user in Context.Guild.Users)
            {
                AprilFoolsUser(user);
            }

        }

        private async Task AprilFoolsUser(SocketGuildUser user)
        {
            aprilFoolsUsers = new Dictionary<ulong, userImage>();
            var account = UserAccounts.GetAccount(user);
            aprilFoolsUsers.Add(user.Id,
                new userImage()
                {
                    id = user.Id,
                    name = user.DisplayName(),
                    element = account.element,
                    classToggle = account.classToggle
                });

            _ = user.ModifyAsync(p => p.Nickname = "Issac");
            await (user as IGuildUser).RemoveRolesAsync(new IRole[] { venusRole, marsRole, jupiterRole, mercuryRole, exathi });
            _ = (user as IGuildUser).AddRoleAsync(venusRole);

            account.element = 0;
            account.classToggle = 0;
        }

        private async Task Client_UserJoined(SocketGuildUser user)
        {
            AprilFoolsUser(user);
        }

        [Command("AprilFoolsOff")]
        public async Task aprilFoolsOff()
        {
            var venusRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Venus Adepts");
            var marsRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Mars Adepts");
            var jupiterRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Jupiter Adepts");
            var mercuryRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Mercury Adepts");
            var exathi = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Exathi") ?? venusRole;

            foreach (SocketGuildUser user in Context.Guild.Users)
            {
                var account = UserAccounts.GetAccount(user);
                if(aprilFoolsUsers.TryGetValue(user.Id, out userImage UI))
                {
                    _ = user.ModifyAsync(p => p.Nickname = UI.name);
                    account.element = UI.element;
                    account.classToggle = UI.classToggle;
                    SocketRole role;
                    switch ((int) account.element)
                    {
                        case 0: role = venusRole;
                            break;
                        case 1: role = marsRole;
                            break;
                        case 2: role = jupiterRole;
                            break;
                        case 3: role = mercuryRole;
                            break;
                        default: role = exathi;
                            break;
                    }
                    _ = user.AddRoleAsync(role);
                }
            }
        }
    }
}