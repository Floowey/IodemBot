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
using Iodembot.Preconditions;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules.ColossoBattles;

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

        [Command("mock")]
        [RequireModerator]
        [Summary("Are you me?")]
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
        [Summary("Pong")]
        public async Task Ping()
        {
            await Context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithColor(Colors.Get("Iodem"))
                .WithDescription($"Pong! {Global.Client.Latency}")
                .Build());
        }

        [Command("pong")]
        [Cooldown(5)]
        [Summary("Ping")]
        public async Task Pong()
        {
            await Context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithColor(Colors.Get("Iodem"))
                .WithDescription($"Ping!")
                .Build());
        }

        [Command("Music")]
        [Cooldown(5)]
        [Summary("Golden Sun Soundtracks to listen to")]
        public async Task Music()
        {
            await Context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithColor(Colors.Get("Iodem"))
                .WithDescription($"While we do not have a dedicated music bot, check out these fantastic playlists to listen to during your adventure:")
                .AddField("OCRemix of the Golden Sun Soundtrack on Soundcloud (it's marvellous)", "https://soundcloud.com/ocremix/sets/golden-sun-a-world-reignited")
                .AddField("Original Sound Track playlist on YouTube:", "https://www.youtube.com/watch?v=rl16-7wZmFY&list=PLCD5E70634946E090")
                .Build());
        }

        [Command("FAQ"), Alias("Changelog", "Links", "Support", "Repo")]
        public async Task FAQ()
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

        [Command("Credit"), Alias("Credits", "Info")]
        public async Task Credit()
        {
            await ReplyAsync(embed: new EmbedBuilder()
                .WithTitle("Credits")
                .WithDescription("Iodem is a community project, designed to add a special, custom made battle system, that mirrors the battle system present in the GBA games to the /r/GoldenSun discord.")
                .AddField("Project Lead", "Floowey")
                .AddField("Co Producers", "Falgor, Gray, Primrose, Ultimastrike")
                .AddField("Art Contributions", "bringobrongo, elTeh, Eon, Mimibits, Shawn, SpaceShaman, Virize, Volk")
                .AddField("Contributions and Testers", "AlterEgo, Arcblade, ArcanusHaru, Dracobolt, DroneberryPi, Germaniac, IceFireFish, joschlumpf, Lavtiz, MarcAustria, Ninja Frog, Ophi, Smeecko, Random, RupeeHeart")
                .AddField("Special thanks to", "Camelot, the Moderators, the Nut Council and you, the players, without whom this whole project wouldn't have come this far")
                .AddField("Support and Links", "Check out the repository:\nhttps://github.com/Floowey/IodemBot/\nIf you want to support this, you can buy Floowey a Ko-Fi!\n https://ko-fi.com/floowey")
                .WithThumbnailUrl("https://cdn.discordapp.com/attachments/668443234292334612/738400124497035284/5ca5bf1dff3c03fbf7cc9b3c_Kofi_logo_RGB_rounded.png")
                .Build());
        }

        [Command("wiki")]
        [Cooldown(5)]
        [Summary("Link to the wiki or a a specific search query.")]
        public async Task Wiki([Remainder] string searchQuery = "")
        {
            string link = "https://goldensunwiki.net/wiki/Main_Page";
            if (searchQuery != "")
            {
                link = $"https://goldensunwiki.net/w/index.php?Search&search={searchQuery.Trim().Replace(" ", "+")}";
            }

            await Context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithColor(Colors.Get("Iodem"))
                .WithDescription(link)
                .Build());
        }

        [Command("subreddit"), Alias("sub")]
        [Cooldown(5)]
        [Summary("Link the wiki")]
        public async Task Subreddit()
        {
            await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                .WithColor(Colors.Get("Iodem"))
                .WithDescription($"https://reddit.com/r/GoldenSun")
                .Build());
        }

        [Command("Game"), Alias("ChangeGame", "SetGame")]
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
            var channel = await Context.Guild.Users.Where(u => u.Id == 300339714311847936).First().GetOrCreateDMChannelAsync();
            await channel.SendMessageAsync($"{Context.User.Username} reports: {bugreport}");
            await Context.Channel.SendMessageAsync($"Thank you for your feedback!");
        }

        [Command("uptime")]
        [Cooldown(60)]
        [Summary("How long has the bot been running")]
        public async Task Uptime()
        {
            await Context.Channel.SendMessageAsync(embed: new EmbedBuilder()
            .WithColor(Colors.Get("Iodem"))
            .AddField("Running since", $"{Global.RunningSince.ToLocalTime()} ({DateTime.Now.Subtract(Global.RunningSince.ToLocalTime()):d' 'hh':'mm':'ss})")
            .AddField("Connected since", $"{Global.UpSince.ToLocalTime()} ({DateTime.Now.Subtract(Global.UpSince.ToLocalTime()):d' 'hh':'mm':'ss})")
            .AddField("Running on", RuntimeInformation.OSDescription)
            .Build());
        }

        [Command("clock"), Alias("worldclock")]
        [Summary("View the current time across the globe")]
        public async Task Worldclock(int time = 24)
        {
            CultureInfo enAU = new CultureInfo("en-US");
            string format = "HH':'mm', 'MMM dd";
            if(time == 12)
            {
                format = "hh':'mm t'M , 'MMM dd";
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await Context.Channel.SendMessageAsync("", false,
                    new EmbedBuilder()
                    .AddField(":globe_with_meridians: UTC", DateTime.UtcNow.ToString(format, enAU), true)
                    .AddField(":flag_at: Vienna", DateTime.Now.ToString(format, enAU), true)
                    .AddField(":flag_in: Mumbai", TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "India Standard Time").ToString(format, enAU), true)
                    .AddField(":flag_jp: Tokyo", TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "Tokyo Standard Time").ToString(format, enAU), true)
                    .AddField(":bridge_at_night: San Francisco", TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "Pacific Standard Time").ToString(format, enAU), true)
                    .AddField(":statue_of_liberty: New York", TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "Eastern Standard Time").ToString(format, enAU), true)
                    .Build()
                    );
            }
            else
            {
                await Context.Channel.SendMessageAsync("", false,
                   new EmbedBuilder()
                   .AddField(":globe_with_meridians: UTC", DateTime.UtcNow.ToString(format, enAU), true)
                   .AddField(":flag_at: Vienna", DateTime.Now.ToString(format, enAU), true)
                   .AddField(":flag_in: New Delhi", TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "Asia/Kolkata").ToString(format, enAU), true)
                   .AddField(":flag_jp: Tokyo", TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "Asia/Tokyo").ToString(format, enAU), true)
                   .AddField(":bridge_at_night: San Francisco", TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "America/Vancouver").ToString(format, enAU), true)
                   .AddField(":statue_of_liberty: New York", TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "America/New_York").ToString(format, enAU), true)
                   .Build()
                   );
            }
        }

        [Command("roleinfo")]
        [Cooldown(10)]
        [Summary("Get information on a specific role")]
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

        [Command("Usercount"), Alias("members", "membercount")]
        [Cooldown(15)]
        [Summary("Display the number of users")]
        public async Task CountUsers()
        {
            var count = Context.Guild.MemberCount;
            var online = Context.Guild.Users.Where(u => u.Status != UserStatus.Offline).Count();

            await Context.Channel.SendMessageAsync(embed: new EmbedBuilder()
            .WithColor(Colors.Get("Iodem"))
            .WithDescription($"{count} Users with {online} Online")
            .Build());
        }

        [Command("choose"), Alias("pick")]
        [Cooldown(15)]
        [Summary("Choose from several words or group of words seperated by ','")]
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
            var choice = choices[(new Random()).Next(0, choices.Length)];
            await Context.Channel.SendMessageAsync(embed: new EmbedBuilder()
            .WithColor(Colors.Get("Iodem"))
            .WithDescription($"➡️ {choice}")
            .Build());
        }

        [Command("rank"), Alias("top", "top10")]
        [Cooldown(15)]
        [Summary("Get the most active users and your rank")]
        public async Task Rank()
        {
            var topAccounts = UserAccounts.GetTop(RankEnum.Level);
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            string[] Emotes = new string[] { "🥇", "🥈", "🥈", "🥉", "🥉", "🥉", "   ", "   ", "   ", "   " };
            var builder = new StringBuilder();
            for (int i = 0; i < Math.Min(topAccounts.Count(), 10); i++)
            {
                var curAccount = topAccounts[i];
                builder.Append($"`{i + 1}` {Emotes[i]} {curAccount.Name?.PadRight(15) ?? curAccount.ID.ToString()} - `Lv{curAccount.LevelNumber}` - `{curAccount.XP}xp`{(curAccount.NewGames > 1 ? $"- `({curAccount.TotalXP}xp total)`" : "")}\n");
            }

            var rank = UserAccounts.GetRank(Context.User);
            //Console.WriteLine(rank);
            var account = UserAccounts.GetAccount(Context.User);
            if (rank >= 10)
            {
                builder.Append("... \n");
                builder.Append($"`{rank + 1}` {Context.User.Username,-15} - `Lv{account.LevelNumber}` - `{account.XP}xp`");
            }

            embed.WithDescription(builder.ToString());

            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("streak"), Alias("showdown")]
        [Summary("Ranking of endless battles")]
        [Cooldown(15)]
        public async Task Showdown(RankEnum type = RankEnum.Solo, EndlessMode mode = EndlessMode.Default)
        {
            var topAccounts = UserAccounts.GetTop(type, mode).Take(10);
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            string[] Emotes = new string[] { "🥇", "🥈", "🥉", "", "" };
            var builder = new StringBuilder();
            for (int i = 0; i < Math.Min(topAccounts.Count(), 5); i++)
            {
                var curAccount = topAccounts.ElementAt(i);
                var streak = mode == EndlessMode.Default ? curAccount.ServerStats.EndlessStreak + curAccount.ServerStatsTotal.EndlessStreak : curAccount.ServerStats.LegacyStreak + curAccount.ServerStats.EndlessStreak;
                switch (type)
                {
                    case RankEnum.Solo:
                        builder.Append($"`{i + 1}` {Emotes[i]} {curAccount.Name,-15} - `{streak.Solo}`\n");
                        break;

                    case RankEnum.Duo:
                        builder.Append($"`{i + 1}` {Emotes[i]} {streak.DuoNames} - `{streak.Duo}`\n");
                        break;

                    case RankEnum.Trio:
                        builder.Append($"`{i + 1}` {Emotes[i]} {streak.TrioNames} - `{streak.Trio}`\n");
                        break;

                    case RankEnum.Quad:
                        builder.Append($"`{i + 1}` {Emotes[i]} {streak.Quad} - `{streak.QuadNames}\n");
                        break;
                }
            }

            var rank = UserAccounts.GetRank(Context.User, type);
            //Console.WriteLine(rank);
            var account = UserAccounts.GetAccount(Context.User);
            if (rank >= 5)
            {
                builder.Append("... \n");
                var streak = mode == EndlessMode.Default ? account.ServerStats.EndlessStreak + account.ServerStatsTotal.EndlessStreak : account.ServerStats.LegacyStreak + account.ServerStats.EndlessStreak;
                switch (type)
                {
                    case RankEnum.Solo:
                        builder.Append($"`{rank + 1}` {account.Name,-15} - `{streak.Solo}`");
                        break;

                    case RankEnum.Duo:
                        builder.Append($"`{rank + 1}` {streak.DuoNames} - `{streak.Duo}`");
                        break;

                    case RankEnum.Trio:
                        builder.Append($"`{rank + 1}` {streak.TrioNames} - `{streak.Trio}`");
                        break;

                    case RankEnum.Quad:
                        builder.Append($"`{rank + 1}` {streak.QuadNames} - `{streak.Quad}`");
                        break;
                }
            }
            if (type == RankEnum.Solo && mode == EndlessMode.Legacy)
            {
                embed.WithFooter("Honorary Mention: Smeecko - 81, by breaking the Time-Space Continuum");
            }
            embed.WithDescription(builder.ToString());

            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("giveRole")]
        [Cooldown(60)]
        [Summary("Give or remove the `Gladiator` or `Colosso Adept` role")]
        public async Task GiveRole([Remainder] string RoleName = "")
        {
            var user = (SocketGuildUser)Context.User;
            var embed = new EmbedBuilder().WithColor(Colors.Get("Iodem")).WithThumbnailUrl(Sprites.GetImageFromName("Iodem"));
            Dictionary<string, ulong> roles = new Dictionary<string, ulong>(StringComparer.CurrentCultureIgnoreCase)
            {
                {"Gladiator", 511704880122036234},
                {"Colosso Adept", 644506247521107969 }
            };

            if(RoleName == "Gladiator" && UserAccounts.GetAccount(Context.User).LevelNumber < 5)
            {
                _ = ReplyAsync("Please participate in the server more before you can announce your streams. We would like to be a community and not just be used as an advertising platform!");
                return;
            }

            if (roles.TryGetValue(RoleName, out ulong roleId))
            {
                var Role = Context.Guild.GetRole(roleId);
                if (user.Roles.Any(r => r.Id == roleId))
                {
                    embed.WithDescription($"{user.DisplayName()} is no longer a {Role.Name}!");
                    await user.RemoveRoleAsync(Role);
                }
                else
                {
                    embed.WithDescription($"{user.DisplayName()} is a {Role.Name} now!");
                    await user.AddRoleAsync(Role);
                }
            }
            else
            {
                embed.WithDescription($"Select any of the following available roles:\n```\n{string.Join("\n", roles.Keys)}```");
            }
            await Context.Channel.SendMessageAsync(embed: embed.Build());
        }

        [Command("addQuote")]
        [RequireModerator]
        [Summary("Add a Quote to the quoteList.")]
        public async Task AddQuoteCommand(string name, [Remainder] string quote)
        {
            Quotes.AddQuote(name, quote);
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithDescription(Utilities.GetAlert("quote_added"));
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }
        [Command("sprite"), Alias("portrait")]
        [Summary("Get a random sprite or one of a given Character")]
        [Cooldown(5)]
        public async Task Sprite([Remainder] string name = "")
        {
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));

            if (Sprites.GetSpriteCount() == 0)
            {
                embed.WithDescription(Utilities.GetAlert("no_sprites"));
            }
            else if (name == "")
            {
                embed.WithImageUrl(Sprites.GetRandomSprite());
            }
            else
            {
                embed.WithImageUrl(Sprites.GetImageFromName(name));
            }

            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }
        [Command("quote"), Alias("q")]
        [Cooldown(10)]
        [Summary("Get a random quote. Add a name to get a quote from that character")]
        public async Task RandomQuote([Remainder] string name = "")
        {
            if (Quotes.GetQuotesCount() == 0)
            {
                await NoQuotes();
                return;
            }

            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));

            var q = Quotes.quoteList.Random();
            if (!name.IsNullOrEmpty())
            {
                q = Quotes.quoteList.Where(q => q.name.Equals(name, StringComparison.OrdinalIgnoreCase)).Random();
                if (q.name.IsNullOrEmpty())
                {
                    embed.WithDescription(Utilities.GetAlert("No_Quote_From_Name"));
                    await ReplyAsync(embed: embed.Build());
                    return;
                }
            }

            q.name = Utilities.ToCaps(q.name);

            embed.WithAuthor(q.name);
            embed.WithThumbnailUrl(Sprites.GetImageFromName(q.name));
            embed.WithDescription(q.quote);
            if (q.quote.Contains(@"#^@%!"))
            {
                var userAccount = UserAccounts.GetAccount(Context.User);
                await GoldenSun.AwardClassSeries("Curse Mage Series", Context.User, Context.Channel);
            }

            await Context.Channel.SendMessageAsync("", false, embed.Build());
            //await embed.WithDescription(Utilities.GetAlert("quote"));
        }

        private async Task NoQuotes()
        {
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithDescription(Utilities.GetAlert("no_quotes"));
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }
    }
}