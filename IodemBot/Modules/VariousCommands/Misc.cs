using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IodemBot.Core;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Preconditions;

namespace IodemBot.Modules
{
    public class Misc : ModuleBase<SocketCommandContext>
    {
        [Command("say")]
        [RequireModerator]
        [Summary("Are you me?")]
        public async Task Echo([Remainder] string message)
        {
            await Context.Message.DeleteAsync();
            await Context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithColor(Colors.Get("Iodem"))
                .WithDescription(message)
                .Build());
        }

        [Command("ping")]
        [Cooldown(5)]
        [Summary("Pong")]
        public async Task PingCommand()
        {
            _ = PingAsync(IIodemCommandContext.GetContext(Context));
            await Task.CompletedTask;
        }

        [Command("CommandDidNotWork")]
        public async Task CommandFailed()
        {
            await ReplyAsync("I can't help you with that");
        }

        public async Task PingAsync(IIodemCommandContext context)
        {
            var cb = new ComponentBuilder();
            cb.WithButton("Ping", "my_id");
            await context.ReplyAsync(embed: new EmbedBuilder()
                .WithColor(Colors.Get("Iodem"))
                .WithDescription($"Pong! {Global.Client.Latency} ms")
                .Build(), component: cb.Build());
        }

        [Command("pong")]
        [Cooldown(5)]
        [Summary("Ping")]
        public async Task Pong()
        {
            await Context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithColor(Colors.Get("Iodem"))
                .WithDescription("Ping!")
                .Build());
        }

        [Command("Music")]
        [Cooldown(5)]
        [Summary("Golden Sun Soundtracks to listen to")]
        public async Task Music()
        {
            await Context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithColor(Colors.Get("Iodem"))
                .WithDescription(
                    "While we do not have a dedicated music bot, check out these fantastic playlists to listen to during your adventure:")
                .AddField("OCRemix of the Golden Sun Soundtrack on Soundcloud (it's marvellous)",
                    "https://soundcloud.com/ocremix/sets/golden-sun-a-world-reignited")
                .AddField("Original Sound Track playlist on YouTube:",
                    "https://www.youtube.com/watch?v=rl16-7wZmFY&list=PLCD5E70634946E090")
                .Build());
        }

        [Command("FAQ")]
        [Alias("Changelog", "Links", "Support", "Repo")]
        public async Task Faq()
        {
            await Context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithColor(Colors.Get("Iodem"))
                .WithDescription("Here's a handful of useful links:")
                .AddField("FAQ", "https://github.com/Floowey/IodemBot/wiki/FAQ")
                .AddField("Changelog", "https://github.com/Floowey/IodemBot/wiki/Changelog")
                .AddField("Repository", "https://github.com/Floowey/IodemBot")
                .AddField("Support", "https://ko-fi.com/floowey")
                .Build());
        }

        [Command("Credit")]
        [Alias("Credits", "Info")]
        public async Task Credit()
        {
            await ReplyAsync(embed: new EmbedBuilder()
                .WithTitle("Credits")
                .WithDescription(
                    "Iodem is a community project, designed to add a special, custom made battle system, that mirrors the battle system present in the GBA games to the /r/GoldenSun discord.")
                .AddField("Project Lead", "Floowey")
                .AddField("Co Producers", "Falgor, Gray, Estelle, Arcblade")
                .AddField("Art Contributions",
                    "bringobrongo, Calvin, elTeh, Eon, generalFang15, Mimibits, Shawn, SpaceShaman, Tert, Virize, Volk, Von")
                .AddField("Contributions and Testers",
                    "AlterEgo, ArcanusHaru, BdeBock, Dracobolt, DroneberryPi, Germaniac, IceFireFish, joschlumpf, Lavtiz, Mary A. Stria, Ninja Frog, Ophi, Smeecko, Random, RupeeHeart, Ultimastrike")
                .AddField("Special thanks to",
                    "Camelot, the Moderators, the Nut Council and you, the players, without whom this whole project wouldn't have come this far")
                .AddField("Support and Links",
                    "Check out the repository:\nhttps://github.com/Floowey/IodemBot/\nIf you want to support this, you can buy Floowey a Ko-Fi!\n https://ko-fi.com/floowey")
                .WithThumbnailUrl(
                    "https://cdn.discordapp.com/attachments/668443234292334612/738400124497035284/5ca5bf1dff3c03fbf7cc9b3c_Kofi_logo_RGB_rounded.png")
                .Build());
        }

        [Command("wiki")]
        [Cooldown(5)]
        [Summary("Link to the wiki or a a specific search query.")]
        public async Task Wiki([Remainder] string searchQuery = "")
        {
            var link = "https://goldensunwiki.net/wiki/Main_Page";
            if (searchQuery != "")
                link = $"https://goldensunwiki.net/w/index.php?Search&search={searchQuery.Trim().Replace(" ", "+")}";

            await Context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithColor(Colors.Get("Iodem"))
                .WithDescription(link)
                .Build());
        }

        [Command("subreddit")]
        [Alias("sub")]
        [Cooldown(5)]
        [Summary("Link the wiki")]
        public async Task Subreddit()
        {
            await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                .WithColor(Colors.Get("Iodem"))
                .WithDescription("https://reddit.com/r/GoldenSun")
                .Build());
        }

        [Command("Game")]
        [Alias("ChangeGame", "SetGame")]
        [Summary("Change what the bot is currently playing.")]
        [RequireOwner]
        public async Task SetGame([Remainder] string gamename)
        {
            await Context.Client.SetGameAsync(gamename);
            await ReplyAsync($"Changed game to `{gamename}`");
        }

        [Command("Bug")]
        [Cooldown(60)]
        [Summary("Send a bug report to @Floowey#0205")]
        public async Task BugReport([Remainder] string bugreport)
        {
            await Global.Owner?.SendMessageAsync($"{Context.User.Mention} reports: {bugreport}");

            ISocketMessageChannel guardChannel =
                Global.Client.GetGuild(355558866282348574)?.GetTextChannel(535209634408169492);
            if (guardChannel != null)
                await guardChannel.SendMessageAsync($"{Context.User.Mention} reports: {bugreport}");

            await Context.Channel.SendMessageAsync("Thank you for your feedback!");
        }

        [Command("uptime")]
        [Cooldown(60)]
        [Summary("How long has the bot been running")]
        public async Task Uptime()
        {
            await Context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithColor(Colors.Get("Iodem"))
                .AddField("Running since",
                    $"{Global.RunningSince.ToLocalTime()} ({DateTime.Now.Subtract(Global.RunningSince.ToLocalTime()):d' 'hh':'mm':'ss})")
                .AddField("Connected since",
                    $"{Global.UpSince.ToLocalTime()} ({DateTime.Now.Subtract(Global.UpSince.ToLocalTime()):d' 'hh':'mm':'ss})")
                .AddField("Running on", RuntimeInformation.OSDescription)
                .Build());
        }

        [Command("clock")]
        [Alias("worldclock")]
        [Summary("View the current time across the globe")]
        public async Task Worldclock(int time = 24)
        {
            var enAu = new CultureInfo("en-US");
            var format = "HH':'mm', 'MMM dd";
            if (time == 12) format = "hh':'mm t'M, 'MMM dd";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                await Context.Channel.SendMessageAsync("", false,
                    new EmbedBuilder()
                        .AddField(":globe_with_meridians: UTC", DateTime.UtcNow.ToString(format, enAu), true)
                        .AddField(":flag_at: Vienna", DateTime.Now.ToString(format, enAu), true)
                        .AddField(":flag_in: Mumbai",
                            TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "India Standard Time")
                                .ToString(format, enAu), true)
                        .AddField(":flag_jp: Tokyo",
                            TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "Tokyo Standard Time")
                                .ToString(format, enAu), true)
                        .AddField(":bridge_at_night: San Francisco",
                            TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "Pacific Standard Time")
                                .ToString(format, enAu), true)
                        .AddField(":statue_of_liberty: New York",
                            TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "Eastern Standard Time")
                                .ToString(format, enAu), true)
                        .Build()
                );
            else
                await Context.Channel.SendMessageAsync("", false,
                    new EmbedBuilder()
                        .AddField(":globe_with_meridians: UTC", DateTime.UtcNow.ToString(format, enAu), true)
                        .AddField(":flag_at: Vienna", DateTime.Now.ToString(format, enAu), true)
                        .AddField(":flag_in: New Delhi",
                            TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "Asia/Kolkata")
                                .ToString(format, enAu), true)
                        .AddField(":flag_jp: Tokyo",
                            TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "Asia/Tokyo")
                                .ToString(format, enAu), true)
                        .AddField(":bridge_at_night: San Francisco",
                            TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "America/Vancouver")
                                .ToString(format, enAu), true)
                        .AddField(":statue_of_liberty: New York",
                            TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "America/New_York")
                                .ToString(format, enAu), true)
                        .Build()
                );
        }

        [Command("roleinfo")]
        [Cooldown(10)]
        [Summary("Get information on a specific role")]
        public async Task RoleInfo([Remainder] string args)
        {
            args = args.ToLower();
            var mentionedRole = Context.Guild.Roles.FirstOrDefault(r => r.Name.ToLower() == args) ?? Context.Guild.Roles.FirstOrDefault(r => r.Name.ToLower().StartsWith(args));
            if (mentionedRole == null || mentionedRole.IsEveryone) return;

            var membercount = Context.Guild.Users.Count(u => u.Roles.Contains(mentionedRole));

            var embed = new EmbedBuilder()
                .WithColor(mentionedRole.Color)
                .WithTitle(mentionedRole.Name)
                .WithThumbnailUrl(Sprites.GetRandomSprite())
                .AddField("ID", mentionedRole.Id, true)
                .AddField("Members", membercount, true)
                .AddField("Color", mentionedRole.Color, true)
                .AddField("Mentionable", mentionedRole.IsMentionable ? "Yes" : "No", true)
                .AddField("Position", mentionedRole.Position, true)
                .AddField("Created", mentionedRole.CreatedAt.Date, true);

            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("Usercount")]
        [Alias("members", "membercount")]
        [Cooldown(15)]
        [Summary("Display the number of users")]
        public async Task CountUsers()
        {
            var count = Context.Guild.MemberCount;
            var online = Context.Guild.Users.Count(u => u.Status != UserStatus.Offline);

            await Context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithColor(Colors.Get("Iodem"))
                .WithDescription($"{count} Users with {online} Online")
                .Build());
        }

        [Command("rank")]
        [Alias("top", "top10")]
        [Cooldown(5)]
        [Summary("Get the most active users and your rank")]
        public async Task Rank(RankEnum type = RankEnum.Month)
        {
            var valid = new[] { RankEnum.AllTime, RankEnum.Month, RankEnum.Week };
            if (!valid.Contains(type))
                return;

            var topAccounts = UserAccounts.GetTop(type);


            // Check Integrity of the leaderboard.
            var topAccountsWeek = topAccounts.Select(u => u.DailyXP
                           .Where(kv => kv.Key.Year == DateTime.Now.Year &&
                           UserAccountProvider.cal.GetWeekOfYear(kv.Key, CalendarWeekRule.FirstDay, DayOfWeek.Monday) == UserAccountProvider.cal.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                           .Select(kv => (decimal)kv.Value).Sum());

            var topAccountsMonth = topAccounts.Select(u => u.DailyXP.Where(kv => kv.Key >= UserAccountProvider.CurrentMonth).Select(kv => (decimal)kv.Value).Sum());
    
            if(topAccountsWeek.ToList().Contains(0) || topAccountsMonth.ToList().Contains(0)){
                Console.WriteLine("Found zeros in monthly or weekly leaderboared. Resetting.");
                UserAccountProvider.ResetLeaderBoards();
                topAccounts = UserAccounts.GetTop(type);
            }

            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));


            string[] emotes = { "🥇", "🥈", "🥈", "🥉", "🥉", "🥉", "   ", "   ", "   ", "   " };
            var builder = new StringBuilder();
            for (var i = 0; i < Math.Min(topAccounts.Count, 10); i++)
            {
                var curAccount = topAccounts[i];
                string toAdd = "";
                switch (type)
                {
                    case RankEnum.AllTime:
                        toAdd = $"`{curAccount.Xp}xp`{(curAccount.NewGames >= 1 ? $"- `({curAccount.TotalXp}xp total)`" : "")}";
                        break;

                    case RankEnum.Week:
                        var xp = (ulong)curAccount.DailyXP
                           .Where(kv => kv.Key.Year == DateTime.Now.Year &&
                           UserAccountProvider.cal.GetWeekOfYear(kv.Key, CalendarWeekRule.FirstDay, DayOfWeek.Monday) == UserAccountProvider.cal.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                           .Select(kv => (decimal)kv.Value).Sum();
                        toAdd = $"`{xp} xp`";
                        break;

                    case RankEnum.Month:
                        xp = (ulong)curAccount.DailyXP.Where(kv => kv.Key >= UserAccountProvider.CurrentMonth).Select(kv => (decimal)kv.Value).Sum();
                        toAdd = $"`{xp} xp`";
                        break;
                }
                builder.Append(
                    $"`{i + 1}` {emotes[i]} {curAccount.Name?.PadRight(15) ?? curAccount.Id.ToString()} - `Lv{curAccount.LevelNumber}` - {toAdd}\n");
            }

            //Console.WriteLine(rank);
            var account = EntityConverter.ConvertUser(Context.User);
            var rank = UserAccounts.GetRank(account, type);
            if (rank >= 10)
            {
                builder.Append("... \n");
                string toAdd = "";
                switch (type)
                {
                    case RankEnum.AllTime:
                        toAdd = $"`{account.Xp}xp`{(account.NewGames >= 1 ? $"- `({account.TotalXp}xp total)`" : "")}";
                        break;

                    case RankEnum.Week:
                        var xp = (ulong)account.DailyXP
                           .Where(kv => kv.Key.Year == DateTime.Now.Year &&
                           UserAccountProvider.cal.GetWeekOfYear(kv.Key, CalendarWeekRule.FirstDay, DayOfWeek.Monday) == UserAccountProvider.cal.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                           .Select(kv => (decimal)kv.Value).Sum();
                        toAdd = $"`{xp} xp`";
                        break;

                    case RankEnum.Month:
                        xp = (ulong)account.DailyXP.Where(kv => kv.Key >= UserAccountProvider.CurrentMonth).Select(kv => (decimal)kv.Value).Sum();
                        toAdd = $"`{xp} xp`";
                        break;
                }
                builder.Append(
                    $"`{rank + 1}` {Context.User.Username,-15} - `Lv{account.LevelNumber}` - {toAdd}\n");
            }

            embed.WithDescription(builder.ToString());
            embed.WithFooter($"Leaderboard of {type}");
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("streak")]
        [Alias("showdown")]
        [Summary("Ranking of endless battles")]
        [Cooldown(5)]
        public async Task Showdown(RankEnum type = RankEnum.Solo, EndlessMode mode = EndlessMode.Default)
        {
            var topAccounts = UserAccounts.GetTop(type, mode);

            if (type == RankEnum.Solo)
                topAccounts = topAccounts.OrderByDescending(d =>
                    (d.ServerStats.GetStreak(mode) + d.ServerStatsTotal.GetStreak(mode)).Solo).ToList();
            else
                topAccounts = topAccounts.Where(p =>
                        (p.ServerStats.GetStreak(mode) + p.ServerStatsTotal.GetStreak(mode)).GetEntry(type).Item1 > 0)
                    .GroupBy(p =>
                        (p.ServerStats.GetStreak(mode) + p.ServerStatsTotal.GetStreak(mode)).GetEntry(type).Item2)
                    .Select(group => group.First())
                    .OrderByDescending(d =>
                        (d.ServerStats.GetStreak(mode) + d.ServerStatsTotal.GetStreak(mode)).GetEntry(type).Item1)
                    .ToList();
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            string[] emotes = { "🥇", "🥈", "🥉", "", "" };
            var builder = new StringBuilder();
            for (var i = 0; i < Math.Min(topAccounts.Count, 5); i++)
            {
                var curAccount = topAccounts.ElementAt(i);
                var streak = mode == EndlessMode.Default
                    ? curAccount.ServerStats.EndlessStreak + curAccount.ServerStatsTotal.EndlessStreak
                    : curAccount.ServerStats.LegacyStreak + curAccount.ServerStatsTotal.LegacyStreak;
                switch (type)
                {
                    case RankEnum.Solo:
                        builder.Append($"`{i + 1}` {emotes[i]} {curAccount.Name,-15} - `{streak.Solo}`\n");
                        break;

                    case RankEnum.Duo:
                        builder.Append($"`{i + 1}` {emotes[i]} {streak.DuoNames} - `{streak.Duo}`\n");
                        break;

                    case RankEnum.Trio:
                        builder.Append($"`{i + 1}` {emotes[i]} {streak.TrioNames} - `{streak.Trio}`\n");
                        break;

                    case RankEnum.Quad:
                        builder.Append($"`{i + 1}` {emotes[i]} {streak.QuadNames} - `{streak.Quad}`\n");
                        break;
                }
            }

            //Console.WriteLine(rank);
            var account = EntityConverter.ConvertUser(Context.User);
            var rank = UserAccounts.GetRank(account, type, mode);
            if (rank >= 5)
            {
                builder.Append("... \n");
                var streak = mode == EndlessMode.Default
                    ? account.ServerStats.EndlessStreak + account.ServerStatsTotal.EndlessStreak
                    : account.ServerStats.LegacyStreak + account.ServerStatsTotal.LegacyStreak;
                switch (type)
                {
                    case RankEnum.Solo:
                        builder.Append($"`{rank + 1}` {account.Name,-15} - `{streak.Solo}`");
                        break;

                    case RankEnum.Duo:
                        builder.Append($"{streak.DuoNames} - `{streak.Duo}`");
                        break;

                    case RankEnum.Trio:
                        builder.Append($"{streak.TrioNames} - `{streak.Trio}`");
                        break;

                    case RankEnum.Quad:
                        builder.Append($"{streak.QuadNames} - `{streak.Quad}`");
                        break;
                }
            }

            if (type == RankEnum.Solo && mode == EndlessMode.Legacy)
                embed.WithFooter("Honorary Mention: Smeecko - 81, by breaking the Time-Space Continuum");
            embed.WithDescription(builder.ToString());

            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("giveRole")]
        [Cooldown(60)]
        [Summary("Give or remove the `Gladiator` or `Colosso Adept` role")]
        public async Task GiveRole([Remainder] string roleName = "")
        {
            var user = (SocketGuildUser)Context.User;
            var embed = new EmbedBuilder().WithColor(Colors.Get("Iodem"))
                .WithThumbnailUrl(Sprites.GetImageFromName("Iodem"));
            var roles = new Dictionary<string, ulong>(StringComparer.CurrentCultureIgnoreCase)
            {
                {"Gladiator", 511704880122036234},
                {"Colosso Adept", 644506247521107969},
                {"Fighter", GuildSettings.GetGuildSettings(Context.Guild).FighterRole.Id}
            };

            if (roleName.Equals("Gladiator", StringComparison.CurrentCultureIgnoreCase) &&
                EntityConverter.ConvertUser(Context.User).LevelNumber < 5)
            {
                _ = ReplyAsync(
                    "Please participate in the server more before you can announce your streams. We would like to be a community and not just be used as an advertising platform!");
                return;
            }

            if (roles.TryGetValue(roleName, out var roleId))
            {
                var role = Context.Guild.GetRole(roleId);
                if (user.Roles.Any(r => r.Id == roleId))
                {
                    embed.WithDescription($"{user.DisplayName()} is no longer a {role.Name}!");
                    await user.RemoveRoleAsync(role);
                }
                else
                {
                    embed.WithDescription($"{user.DisplayName()} is a {role.Name} now!");
                    await user.AddRoleAsync(role);
                }
            }
            else
            {
                embed.WithDescription(
                    $"Select any of the following available roles:\n```\n{string.Join("\n", roles.Keys)}```");
            }

            await Context.Channel.SendMessageAsync(embed: embed.Build());
        }

        [Command("sprite")]
        [Alias("portrait")]
        [Summary("Get a random sprite or one of a given Character")]
        [Cooldown(5)]
        public async Task Sprite([Remainder] string name = "")
        {
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));

            if (Sprites.GetSpriteCount() == 0)
                embed.WithDescription("No sprites found.");
            else if (name == "")
                embed.WithImageUrl(Sprites.GetRandomSprite());
            else
                embed.WithImageUrl(Sprites.GetImageFromName(name));

            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("quote")]
        [Alias("q")]
        [Cooldown(10)]
        [Summary("Get a random quote. Add a name to get a quote from that character")]
        public async Task RandomQuote([Remainder] string name = "")
        {
            if (Quotes.GetQuotesCount() == 0)
            {
                await ReplyAsync("I don't recall any quotes.");
                return;
            }

            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));

            var q = Quotes.QuoteList.Random();
            if (!name.IsNullOrEmpty())
            {
                q = Quotes.QuoteList.Where(q => q.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).Random();
                if (q.Name.IsNullOrEmpty())
                {
                    embed.WithDescription("I don't remember anything this person said.");
                    await ReplyAsync(embed: embed.Build());
                    return;
                }
            }

            q.Name = Utilities.ToCaps(q.Name);

            embed.WithAuthor(q.Name);
            embed.WithThumbnailUrl(Sprites.GetImageFromName(q.Name));
            embed.WithDescription(q.Quote);
            if (q.Quote.Contains(@"#^@%!"))
            {
                var userAccount = EntityConverter.ConvertUser(Context.User);
                await GoldenSunCommands.AwardClassSeries("Curse Mage Series", Context.User, Context.Channel);
            }

            await ReplyAsync("", false, embed.Build());
        }
    }
}