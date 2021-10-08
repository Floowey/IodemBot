using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IodemBot.Preconditions;
using IodemBot.Core.UserManagement;
using IodemBot.Modules.ColossoBattles;
using Newtonsoft.Json;

namespace IodemBot.Modules
{
    public class ModCommands : ModuleBase<SocketCommandContext>
    {
        [Command("Ban")]
        [RequireModerator]
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task BanUser(IGuildUser user, [Remainder] string reason = "No reason provided.")
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
        [Remarks("Purges A User's Last Messages. Default Amount To Purge is 100")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task Clear(int amountOfMessagesToDelete = 2)
        {
            var messages = await Context.Message.Channel.GetMessagesAsync(amountOfMessagesToDelete + 1).FlattenAsync();
            var result = messages.Where(x => x.CreatedAt >= DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(14)));

            await (Context.Message.Channel as SocketTextChannel).DeleteMessagesAsync(result);
        }

        [Command("Kick")]
        [RequireModerator]
        public async Task KickUser(IGuildUser user, [Remainder] string reason = "No reason provided.")
        {
            await user.KickAsync(reason);
        }

        [Command("setupIodem")]
        [Remarks("One Time Use only, if it works")]
        [RequireOwner]
        public async Task SetupIodem()
        {
            _ = SetupIodemTask();
            await Task.CompletedTask;
        }

        private async Task SetupIodemTask()
        {
            await Context.Guild.DownloadUsersAsync();
            foreach (var gm in Context.Guild.Users)
            {
                var user = EntityConverter.ConvertUser(gm);
                Console.WriteLine($"{user.Name} registered");

                var elRole = gm.Roles.FirstOrDefault(r => r.Name.Contains("Adepts"));
                if(elRole != null && user.Element == Element.none)
                {
                    var chosenElement = new[] { Element.Venus, Element.Mars, Element.Jupiter, Element.Mercury }
                        .First(e => elRole.Name.Contains(e.ToString()));
                    user.Element = chosenElement;
                    var tags = new[] { "VenusAdept", "MarsAdept", "JupiterAdept", "MercuryAdept" };
                    user.Tags.RemoveAll(s => tags.Contains(s));
                    user.Tags.Add(tags[(int)chosenElement]);
                    Console.WriteLine($"Updated Element for {user.Name}.");
                }
            }
            await Task.CompletedTask;
        }

        [Command("Activity")]
        [RequireModerator]
        public async Task Activity()
        {
            var acc = UserAccountProvider.GetAllUsers();
            await ReplyAsync("", false, new EmbedBuilder()
                .WithDescription("Server activity")
                .AddField("Total Members ever", acc.Count(), true)
                .AddField("Members now", Context.Guild.MemberCount, true)
                .AddField("24h", acc.Count(a => DateTime.Now.Subtract(new TimeSpan(24, 0, 0)) < a.ServerStats.LastDayActive), true)
                .AddField("3 Days", acc.Count(a => DateTime.Now.Subtract(new TimeSpan(3, 0, 0, 0)) < a.ServerStats.LastDayActive), true)
                .AddField("7 Days", acc.Count(a => DateTime.Now.Subtract(new TimeSpan(7, 0, 0, 0)) < a.ServerStats.LastDayActive), true)
                .AddField("30 Days", acc.Count(a => DateTime.Now.Subtract(new TimeSpan(30, 0, 0, 0)) < a.ServerStats.LastDayActive), true)
                .AddField("All Time", acc.Count(a => a.ServerStats.LastDayActive > DateTime.MinValue), true)
                .AddField("Tried Colosso", acc.Count(a => a.ServerStats.ColossoWins > 0), true)
                .Build());
        }

        [Command("UpdateIodem")]
        [RequireOwner]
        public async Task UpdateSelf()
        {
            await ReplyAsync("Shutting down for automatic update...");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Console.WriteLine($"Closing {Global.Client.CurrentUser.Username } for manual update...");
                var ps = new ProcessStartInfo
                {
                    FileName = "shellscripts/selfupdate.sh",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    Arguments = Global.Client.CurrentUser.Username == "Faran" ? "MedoiBotService" : "IodemBotService"
                };

                Process process = Process.Start(ps);
                process.WaitForExit();
                Console.WriteLine("This shouldn't be reached but did.");
                return;

            }
        }

        [Command("backupusers")]
        [RequireOwner]
        public async Task BackupUsers()
        {
            await ReplyAsync("Manually backing up users...");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Console.WriteLine("Saving Users manually...");
                var ps = new ProcessStartInfo
                {
                    FileName = "shellscripts/backupusers.sh",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                Process process = Process.Start(ps);
                process.WaitForExit();
                return;

            }
        }

        [Command("pullusers")]
        [RequireOwner]
        public async Task PullUsers()
        {
            await ReplyAsync("Restoring users. Restart imminent...");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Console.WriteLine("Closing for automatic update...");
                var ps = new ProcessStartInfo
                {
                    FileName = "shellscripts/pullusers.sh",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    Arguments = Global.Client.CurrentUser.Username == "Faran" ? "MedoiBotService" : "IodemBotService"
                };

                Process process = Process.Start(ps);
                process.WaitForExit();
                return;
            }
        }

        [Command("Tags")]
        [RequireOwner]
        public async Task Tags(SocketGuildUser user)
        {
            var avatar = EntityConverter.ConvertUser(user);
            var tags = string.Join(", ", avatar.Tags);
            while(tags.Length > 0)
            {
                await ReplyAsync(tags.Substring(0, 2000));
                tags = tags.Substring(2000);
            }

        }

        [Command("CleanupTags")]
        [RequireModerator]
        public async Task CleanupTags(SocketGuildUser user)
        {
            var avatar = EntityConverter.ConvertUser(user);
            var allTags1 = EnemiesDatabase.dungeons.Values.SelectMany(c => c.Requirement.TagsAny);
            var allTags2 = EnemiesDatabase.dungeons.Values.SelectMany(c => c.Requirement.TagsLock);
            var allTags3 = EnemiesDatabase.dungeons.Values.SelectMany(c => c.Requirement.TagsRequired);
            var allTags4 = EnemiesDatabase.dungeons.Values.SelectMany(c => c.Matchups.SelectMany(m => m.RewardTables.SelectMany(r => r.Select(w => w.Tag))));
            var allTags5 = EnemiesDatabase.dungeons.Values.SelectMany(c => c.Matchups.SelectMany(m => m.RewardTables.SelectMany(r => r.SelectMany(w => w.RequireTag))));
            var unusedTags = avatar.Tags.Where(t => !allTags1.Contains(t) && !allTags2.Contains(t) && !allTags3.Contains(t) && !allTags4.Contains(t) && !allTags5.Contains(t));

            avatar.Tags.RemoveAll(t => unusedTags.Contains(t));
            UserAccountProvider.StoreUser(avatar);
            await ReplyAsync(string.Join(", ", unusedTags));
        }

        [Command("RemoveTag")]
        [RequireOwner]
        public async Task RemoveTag(SocketGuildUser user, string Tag)
        {
            var avatar = EntityConverter.ConvertUser(user);
            _ = ReplyAsync($"Tag Removed {avatar.Tags.Remove(Tag)}");
            UserAccountProvider.StoreUser(avatar);
            await Task.CompletedTask;
        }

        [Command("SetXP")]
        [RequireOwner]
        public async Task SetXP(SocketGuildUser user, uint xp)
        {
            var avatar = EntityConverter.ConvertUser(user);
            avatar.XP = xp;
            UserAccountProvider.StoreUser(avatar);
            await Task.CompletedTask;
        }

        [Command("AddTag")]
        [RequireOwner]
        public async Task AddTag(SocketGuildUser user, string Tag)
        {
            var avatar = EntityConverter.ConvertUser(user);
            avatar.Tags.Add(Tag);
            UserAccountProvider.StoreUser(avatar);
            await ReplyAsync("Tag Added");
        }

        [Command("Shiny")]
        [RequireOwner]
        public async Task ShinyDjinn(SocketGuildUser user, bool shiny, [Remainder] string djinn)
        {
            var acc = EntityConverter.ConvertUser(user);
            var userDjinn = acc.DjinnPocket;
            var chosenDjinn = userDjinn.GetDjinn(djinn);
            if (chosenDjinn == null)
            {
                return;
            }
            chosenDjinn.IsShiny = shiny;
            UserAccountProvider.StoreUser(acc);
            await Task.CompletedTask;
        }

        [Command("GetUserFile")]
        [RequireStaff]
        public async Task GetUserFile(SocketUser user = null)
        {
            user ??= Context.User;
            await Context.Channel.SendFileAsync($"Resources/Accounts/BackupAccountFiles/{user.Id}.json");    
        }

        [Command("PutUserFile")]
        [RequireStaff]
        public async Task PutUserFile()
        {
            var file = Context.Message.Attachments.FirstOrDefault();
            if (file != null && file.Filename.EndsWith(".json"))
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        var json = await client.GetStringAsync(file.Url);
                        var user = JsonConvert.DeserializeObject<UserAccount>(json);
                        UserAccountProvider.StoreUser(user);
                    }
                    await ReplyAsync($"Successfully updated {Context.User.Mention}");
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            await ReplyAsync("Invalid File");
        }

        [Command("Emotes")]
        [RequireUserPermission(GuildPermission.ManageEmojisAndStickers)]
        public async Task Emotes()
        {
            var s = string.Join("\n", Context.Guild.Emotes.OrderBy(d => d.Name).Select(e => $"{e} \\<{(e.Animated ? "a" : "")}:{e.Name}:{e.Id}>"));
            while (s.Length > 2000)
            {
                await Context.Channel.SendMessageAsync(s.Substring(0, 2000));
                s = s.Substring(2000);
            }

            await Context.Channel.SendMessageAsync(s);

        }
    }
}