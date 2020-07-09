using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Iodembot.Preconditions;
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
        [Remarks("Purges A User's Last Messages. Default Amount To Purge Is 100")]
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
            foreach (SocketGuildUser user in Context.Guild.Users)
            {
                var account = UserAccounts.GetAccount(user);

                account.Name = user.DisplayName();
            }
            UserAccounts.SaveAccounts();
            await Task.CompletedTask;
        }

        [Command("Activity")]
        [RequireOwner]
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
            avatar.Tags.Remove(Tag);
            await ReplyAsync("Tag Removed");
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