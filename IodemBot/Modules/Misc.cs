using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Iodembot.Preconditions;
using IodemBot.Core.UserManagement;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Modules
{
    public class Misc : ModuleBase<SocketCommandContext>
    {
        [Command("say")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        [Remarks("Are you me?")]
        public async Task Echo([Remainder] string message)
        {
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithDescription(message);
            await Context.Message.DeleteAsync();
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("mock")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        [Remarks("Are you me?")]
        public async Task Mock([Remainder] string message)
        {
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithAuthor(Context.User);
            embed.WithDescription(StringToMock(message));
            await Context.Message.DeleteAsync();
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        private string StringToMock(string text)
        {
            var lower = text.ToLower();
            var s = new StringBuilder();
            for (int i = 0; i < lower.Length; i++)
            {
                string c = lower[i].ToString();
                if (i % 2 == 1)
                {
                    c = c.ToUpper();
                }

                s.Append(c);
            }

            return s.ToString();
        }

        [Command("ping")]
        [Cooldown(5)]
        [Remarks("Pong")]
        public async Task Ping()
        {
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithDescription($"Pong!");
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("wiki")]
        [Cooldown(5)]
        [Remarks("Link to the wiki or a a specific search query.")]
        public async Task Wiki([Remainder] string searchQuery = "")
        {
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            string link = "https://goldensunwiki.net/wiki/Main_Page";
            if (searchQuery != "")
            {
                link = $"https://goldensunwiki.net/w/index.php?Search&search={searchQuery.Trim().Replace(" ", "+")}";
            }

            embed.WithDescription(link);
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("subreddit"), Alias("sub")]
        [Cooldown(5)]
        [Remarks("Link the wiki")]
        public async Task Subreddit()
        {
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithDescription($"https://reddit.com/r/GoldenSun");
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("Game"), Alias("ChangeGame", "SetGame")]
        [Remarks("Change what the bot is currently playing.")]
        [RequireOwner]
        public async Task SetGame([Remainder] string gamename)
        {
            await Context.Client.SetGameAsync(gamename);
            await ReplyAsync($"Changed game to `{gamename}`");
        }

        [Command("Bug")]
        [Cooldown(60)]
        [Remarks("Send a bug report to @Floowey#0205")]
        public async Task BugReport([Remainder] string bugreport)
        {
            var channel = await Context.Guild.Users.Where(u => u.Id == 300339714311847936).First().GetOrCreateDMChannelAsync();
            await channel.SendMessageAsync($"{Context.User.Username} reports: {bugreport}");
            await Context.Channel.SendMessageAsync($"Thank you for your feedback!");
        }

        [Command("uptime")]
        [Cooldown(60)]
        [Remarks("How long has the bot been running")]
        public async Task Uptime()
        {
            await Context.Channel.SendMessageAsync($"Running since {Global.UpSince.ToLocalTime()}.");
        }

        [Command("roleinfo")]
        [Cooldown(10)]
        [Remarks("Get information on a specific role")]
        public async Task RoleInfo([Remainder] string args)
        {
            args = args.ToLower();
            var mentionedRole = Context.Guild.Roles.Where(r => r.Name.ToLower() == args).FirstOrDefault();
            if (mentionedRole == null)
            {
                mentionedRole = Context.Guild.Roles.Where(r => r.Name.ToLower().StartsWith(args)).FirstOrDefault();
            }
            if (mentionedRole == null || mentionedRole.IsEveryone)
            {
                return;
            }

            var membercount = Context.Guild.Users.Where(u => u.Roles.Contains(mentionedRole)).Count();

            var embed = new EmbedBuilder();
            embed.WithColor(mentionedRole.Color);
            embed.WithTitle(mentionedRole.Name);
            embed.WithThumbnailUrl(Sprites.GetRandomSprite());

            embed.AddField("ID", mentionedRole.Id, true);
            embed.AddField("Members", membercount, true);
            embed.AddField("Color", mentionedRole.Color, true);
            embed.AddField("Mentionable", mentionedRole.IsMentionable ? "Yes" : "No", true);
            embed.AddField("Position", mentionedRole.Position, true);
            embed.AddField("Created", mentionedRole.CreatedAt.Date, true);
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("Usercount"), Alias("members", "membercount")]
        [Cooldown(15)]
        [Remarks("Display the number of users")]
        public async Task CountUsers()
        {
            var count = Context.Guild.Users.Count;
            var offline = Context.Guild.Users.Where(u => u.Status == UserStatus.Offline).Count();
            var online = count - offline;
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithDescription($"{count} Users with {online} Online");

            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("choose"), Alias("pick")]
        [Cooldown(15)]
        [Remarks("Choose from several words or group of words seperated by ','")]
        public async Task Choose([Remainder] string s)
        {
            var choices = s.Split(' ');
            if (s.Contains(','))
            {
                choices = s.Split(',');
            }
            foreach (string c in choices)
            {
                c.Trim();
            }
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            var choice = choices[(new Random()).Next(0, choices.Length)];
            embed.WithDescription($"➡️ {choice}");
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        public enum RankEnum { Level, Solo, Duo, Trio, Quad }

        [Command("rank"), Alias("top", "top10")]
        [Cooldown(15)]
        [Remarks("Get the most active users and your rank")]
        public async Task Rank()
        {
            var topAccounts = UserAccounts.GetTop(10, RankEnum.Level);
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            string[] Emotes = new string[] { "🥇", "🥈", "🥈", "🥉", "🥉", "🥉", "   ", "   ", "   ", "   " };
            var builder = new StringBuilder();
            for (int i = 0; i < Math.Min(topAccounts.Count(), 10); i++)
            {
                var curAccount = topAccounts[i];
                builder.Append($"`{i + 1}` {Emotes[i]} {curAccount.Name.PadRight(15)} - `Lv{curAccount.LevelNumber}` - `{curAccount.XP}xp`\n");
            }

            var rank = UserAccounts.GetRank(Context.User);
            //Console.WriteLine(rank);
            var account = UserAccounts.GetAccount(Context.User);
            if (rank >= 10)
            {
                builder.Append("... \n");
                builder.Append($"`{rank + 1}` {Context.User.Username.PadRight(15)} - `Lv{account.LevelNumber}` - `{account.XP}xp`");
            }

            embed.WithDescription(builder.ToString());

            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("showdown")]
        [Cooldown(15)]
        public async Task Showdown(RankEnum type = RankEnum.Solo)
        {
            var topAccounts = UserAccounts.GetTop(10, type);
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            string[] Emotes = new string[] { "🥇", "🥈", "🥉", "", "" };
            var builder = new StringBuilder();
            for (int i = 0; i < Math.Min(topAccounts.Count(), 5); i++)
            {
                var curAccount = topAccounts[i];
                switch (type)
                {
                    case RankEnum.Solo:
                        builder.Append($"`{i + 1}` {Emotes[i]} {curAccount.Name.PadRight(15)} - `{curAccount.ServerStats.ColossoHighestRoundEndlessSolo}`\n");
                        break;

                    case RankEnum.Duo:
                        builder.Append($"`{i + 1}` {Emotes[i]} {curAccount.ServerStats.ColossoHighestRoundEndlessDuoNames} - `{curAccount.ServerStats.ColossoHighestRoundEndlessDuo}`\n");
                        break;

                    case RankEnum.Trio:
                        builder.Append($"`{i + 1}` {Emotes[i]} {curAccount.ServerStats.ColossoHighestRoundEndlessTrioNames} - `{curAccount.ServerStats.ColossoHighestRoundEndlessTrio}`\n");
                        break;

                    case RankEnum.Quad:
                        builder.Append($"`{i + 1}` {Emotes[i]} {curAccount.ServerStats.ColossoHighestRoundEndlessQuadNames} - `{curAccount.ServerStats.ColossoHighestRoundEndlessQuad}`\n");
                        break;
                }
            }

            var rank = UserAccounts.GetRank(Context.User, type);
            //Console.WriteLine(rank);
            var account = UserAccounts.GetAccount(Context.User);
            if (rank >= 5)
            {
                builder.Append("... \n");
                switch (type)
                {
                    case RankEnum.Solo:
                        builder.Append($"`{rank + 1}` {account.Name.PadRight(15)} - `{account.ServerStats.ColossoHighestRoundEndlessSolo}`");
                        break;

                    case RankEnum.Duo:
                        builder.Append($"`{rank + 1}` {account.ServerStats.ColossoHighestRoundEndlessDuoNames} - `{account.ServerStats.ColossoHighestRoundEndlessDuo}`");
                        break;

                    case RankEnum.Trio:
                        builder.Append($"`{rank + 1}` {account.ServerStats.ColossoHighestRoundEndlessTrioNames} - `{account.ServerStats.ColossoHighestRoundEndlessTrio}`");
                        break;

                    case RankEnum.Quad:
                        builder.Append($"`{rank + 1}` {account.ServerStats.ColossoHighestRoundEndlessQuadNames} - `{account.ServerStats.ColossoHighestRoundEndlessQuad}`");
                        break;
                }
            }
            embed.WithDescription(builder.ToString());

            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("Gladiator"), Alias("Streamer")]
        [Cooldown(60)]
        [Remarks("Give or remove the Gladiator Role")]
        public async Task Gladiator([Remainder] string rem = "")
        {
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            SocketGuildUser user;
            if (Context.Message.MentionedUsers.FirstOrDefault() != null)
            {
                user = (SocketGuildUser)Context.Message.MentionedUsers.FirstOrDefault();
            }
            else
            {
                user = (SocketGuildUser)Context.User;
            }

            var Role = Context.Guild.Roles.Where(r => r.Id == 511704880122036234).FirstOrDefault();
            if (Role == null)
            {
                return;
            }

            if (Role.Members.Where(m => m.Id == user.Id).FirstOrDefault() == null)
            {
                await user.AddRoleAsync(Role);
            }
            else
            {
                await user.RemoveRoleAsync(Role);
            }

            embed.WithDescription($"{user.Nickname} is a {Role.Name} now!");
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }
    }
}