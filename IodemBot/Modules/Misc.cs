using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Iodembot.Preconditions;
using IodemBot.Core.Leveling;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules.ColossoBattles;
using IodemBot.Modules.GoldenSunMechanics;
using Newtonsoft.Json;
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

        [Command("xp")]
        [Cooldown(5)]
        [Remarks("Get information about your level etc")]
        public async Task Xp()
        {
            var user = (SocketGuildUser)Context.User;
            var account = UserAccounts.GetAccount(user);
            var embed = new EmbedBuilder();
            var p = new PlayerFighter(user);

            embed.WithColor(Colors.Get(account.Element.ToString()));
            var author = new EmbedAuthorBuilder();
            author.WithName(user.DisplayName());
            author.WithIconUrl(user.GetAvatarUrl());
            embed.WithAuthor(author);
            embed.AddField("Level", account.LevelNumber, true);
            embed.AddField("XP", account.XP, true);
            embed.AddField("XP to level up", Leveling.XPforNextLevel(account.XP), true);
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("status")]
        [Cooldown(5)]
        [Remarks("Get information about your level etc")]
        public async Task Status(SocketGuildUser user = null)
        {
            user = user ?? (SocketGuildUser)Context.User;
            var account = UserAccounts.GetAccount(user);
            var embed = new EmbedBuilder();
            var p = new PlayerFighter(user);

            embed.WithColor(Colors.Get(account.Element.ToString()));
            var author = new EmbedAuthorBuilder();
            author.WithName($"{user.DisplayName()}");
            author.WithIconUrl(user.GetAvatarUrl());
            embed.WithAuthor(author);
            //embed.WithThumbnailUrl(user.GetAvatarUrl());
            //embed.WithDescription($"Status.");

            //embed.AddField("Element", account.element, true);

            embed.AddField("Level", account.LevelNumber, true);
            embed.AddField("XP", $"{account.XP} - next in {Leveling.XPforNextLevel(account.XP)}", true);
            embed.AddField("Rank", UserAccounts.GetRank(user) + 1, true);

            embed.AddField("Class", account.GsClass, true);
            embed.AddField("Colosso wins", account.ServerStats.ColossoWins, true);

            embed.AddField("Current Equip", account.Inv.GearToString(AdeptClassSeriesManager.GetClassSeries(account).Archtype), true);
            embed.AddField("Psynergy", p.GetMoves(false), false);

            embed.AddField("Stats", p.stats.ToString(), true);
            embed.AddField("Elemental Stats", p.elstats.ToString(), true);
            embed.AddField("Unlocked Classes", account.BonusClasses.Length == 0 ? "none" : string.Join(", ", account.BonusClasses));

            var Footer = new EmbedFooterBuilder();
            Footer.WithText("Joined this Server on " + user.JoinedAt.Value.Date.ToString("dd-MM-yyyy"));
            Footer.WithIconUrl(Sprites.GetImageFromName("Iodem"));
            embed.WithFooter(Footer);

            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("hiddenstats"), Alias("tri")]
        [Cooldown(5)]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        [Remarks("Hidden Information on Tri-Elemental Classes")]
        public async Task Tri(SocketGuildUser user = null)
        {
            user = user ?? (SocketGuildUser)Context.User;
            var account = UserAccounts.GetAccount(user);
            var embed = new EmbedBuilder();
            var p = new PlayerFighter(user);

            embed.WithColor(Colors.Get(account.Element.ToString()));
            var author = new EmbedAuthorBuilder();
            author.WithName(user.DisplayName());
            author.WithIconUrl(user.GetAvatarUrl());
            embed.WithAuthor(author);
            embed.WithThumbnailUrl(user.GetAvatarUrl());
            //embed.WithDescription($"Status.");

            //embed.AddField("Damage (Ninja)", account.BattleStats.damageDealt, true);
            //embed.AddField("Kills By Hand (Samurai)", account.BattleStats.killsByHand, true);
            //embed.AddField("HP Healed (White Mage)", account.BattleStats.HPhealed, true);
            //embed.AddField("Revives (Medium)", account.BattleStats.revives, true);
            //embed.AddField("Solos (Ranger)", account.BattleStats.soloBattles, true);
            //embed.AddField("Teammates (Dragoon)", account.BattleStats.totalTeamMates, true);
            //embed.AddField("Days Active (Hermit)", account.ServerStats.uniqueDaysActive, true);
            //embed.AddField("Commands Used (Scrapper)", account.ServerStats.CommandsUsed);
            //embed.AddField("Wins/Streak (Brute, Curse Mage and More)", $"{account.ServerStats.ColossoWins}, {account.ServerStats.ColossoHighestStreak}");
            //embed.AddField("Rps Wins/Streak (Aqua/Air Seer)", $"{account.ServerStats.rpsWins}, {account.ServerStats.rpsStreak}");
            //embed.AddField("Channel Switches (Pilgrim)", account.ServerStats.channelSwitches);
            //embed.AddField("Curse Mage (Written Curse, Quoted Matthew)", $"{account.ServerStats.hasWrittenCurse}, {account.ServerStats.hasQuotedMatthew}");

            embed.AddField("Server Stats", JsonConvert.SerializeObject(account.ServerStats, Formatting.Indented));
            embed.AddField("Battle Stats", JsonConvert.SerializeObject(account.BattleStats, Formatting.Indented));

            embed.AddField("Unlocked Classes", account.BonusClasses.Length == 0 ? "none" : string.Join(", ", account.BonusClasses));

            var Footer = new EmbedFooterBuilder();
            Footer.WithText("Joined this Server on " + user.JoinedAt.Value.Date.ToString("dd-MM-yyyy"));
            Footer.WithIconUrl(Sprites.GetImageFromName("Iodem"));
            embed.WithFooter(Footer);

            //await Context.User.SendMessageAsync("", false, embed.Build());
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

        [Command("rank"), Alias("top")]
        [Cooldown(15)]
        [Remarks("Get the most active users and your rank")]
        public async Task Rank()
        {
            var topAccounts = UserAccounts.GetTop(10);
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            string[] Emotes = new string[] { "🥇", "🥈", "🥈", "🥉", "🥉", "🥉", "   ", "   ", "   ", "   " };
            var builder = new StringBuilder();
            for (int i = 0; i < 10; i++)
            {
                var curAccount = topAccounts[i];
                builder.Append($"`{i + 1}` {Emotes[i]} {curAccount.Name.PadRight(15)} - `Lv{curAccount.LevelNumber}` - `{curAccount.XP}xp` \n");
                //builder.Append($"`{i + 1}` {Emotes[i]} - `Lv{curAccount.LevelNumber}` - `{curAccount.XP}xp` \n");
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