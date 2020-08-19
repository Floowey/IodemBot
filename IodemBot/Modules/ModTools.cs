using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Iodembot.Preconditions;
using IodemBot.Core;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules
{
    public class ModTools : ModuleBase<SocketCommandContext>
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
        }

        private async Task SetupIodemTask()
        {
            await Context.Guild.DownloadUsersAsync();
            var UsersWhoTalked = new List<string>();
            var UsersWhoNeverTalked = new List<string>();
            foreach (UserAccount user in UserAccounts.GetAllAccounts())
            {
                if (!Context.Guild.Users.Any(u => u.Id == user.ID))
                {
                    if (user.LevelNumber > 2)
                    {
                        UsersWhoTalked.Add(user.Name);
                    }
                    else
                    {
                        UsersWhoNeverTalked.Add(user.Name);
                    }
                }
            }
            foreach (SocketGuildUser user in Context.Guild.Users)
            {
                var account = UserAccounts.GetAccount(user);

                account.Name = user.DisplayName();
            }
            _ = ReplyAsync(string.Join(", ", UsersWhoTalked));
            _ = ReplyAsync(string.Join(", ", UsersWhoNeverTalked));
            Console.WriteLine(Global.Client.Guilds.Sum(g => g.Emotes.Count));
            UserAccounts.SaveAccounts();
            GuildSettings.SaveGuilds();
            await Task.CompletedTask;
        }

        [Command("Activity")]
        [RequireModerator]
        public async Task Activity()
        {
            var acc = UserAccounts.GetAllAccounts();
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
            var avatar = UserAccounts.GetAccount(user);
            await ReplyAsync(string.Join(", ", avatar.Tags));
        }

        [Command("CleanupTags")]
        [RequireModerator]
        public async Task CleanupTags(SocketGuildUser user)
        {
            var avatar = UserAccounts.GetAccount(user);
            var allTags1 = EnemiesDatabase.dungeons.Values.SelectMany(c => c.Requirement.TagsAny);
            var allTags2 = EnemiesDatabase.dungeons.Values.SelectMany(c => c.Requirement.TagsLock);
            var allTags3 = EnemiesDatabase.dungeons.Values.SelectMany(c => c.Requirement.TagsRequired);
            var allTags4 = EnemiesDatabase.dungeons.Values.SelectMany(c => c.Matchups.SelectMany(m => m.RewardTables.SelectMany(r => r.Select(w => w.Tag))));
            var allTags5 = EnemiesDatabase.dungeons.Values.SelectMany(c => c.Matchups.SelectMany(m => m.RewardTables.SelectMany(r => r.SelectMany(w => w.RequireTag))));
            var unusedTags = avatar.Tags.Where(t => !allTags1.Contains(t) && !allTags2.Contains(t) && !allTags3.Contains(t) && !allTags4.Contains(t) && !allTags5.Contains(t));

            avatar.Tags.RemoveAll(t => unusedTags.Contains(t));
            await ReplyAsync(string.Join(", ", unusedTags));
        }

        [Command("RemoveTag")]
        [RequireOwner]
        public async Task RemoveTag(SocketGuildUser user, string Tag)
        {
            var avatar = UserAccounts.GetAccount(user);
            await ReplyAsync($"Tag Removed {avatar.Tags.Remove(Tag)}");
        }

        [Command("AddTag")]
        [RequireOwner]
        public async Task AddTag(SocketGuildUser user, string Tag)
        {
            var avatar = UserAccounts.GetAccount(user);
            avatar.Tags.Add(Tag);
            await ReplyAsync("Tag Added");
        }

        [Command("Emotes")]
        [RequireStaff]
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