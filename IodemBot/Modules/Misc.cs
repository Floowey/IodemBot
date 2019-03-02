using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Iodembot.Preconditions;
using IodemBot.Core.Leveling;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Modules
{
    public class Misc : ModuleBase<SocketCommandContext>
    { 
        [Command("say")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        [Remarks("Are you me?")]
        public async Task Echo([Remainder] string message) {
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.get("Iodem"));
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
            embed.WithColor(Colors.get("Iodem"));
            embed.WithAuthor(Context.User);
            embed.WithDescription(stringToMock(message));
            await Context.Message.DeleteAsync();
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        private string stringToMock(string text)
        {
            var lower = text.ToLower();
            var s = new StringBuilder();
            for(int i = 0; i < lower.Length; i++)
            {
                string c = lower[i].ToString();
                if (i % 2 == 1) c = c.ToUpper();
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
            embed.WithColor(Colors.get("Iodem"));
            embed.WithDescription($"Pong!");
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("wiki")]
        [Cooldown(5)]
        [Remarks("Link the wiki")]
        public async Task Wiki()
        {
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.get("Iodem"));
            embed.WithDescription($"https://goldensunwiki.net/wiki/Main_Page");
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("subreddit"), Alias("sub")]
        [Cooldown(5)]
        [Remarks("Link the wiki")]
        public async Task Subreddit()
        {
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.get("Iodem"));
            embed.WithDescription($"https://reddit.com/r/GoldenSun");
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("names")]
        public async Task Names()
        {
            SocketGuildUser user = (SocketGuildUser) Context.User;
            await Context.Channel.SendMessageAsync($"{user.Username} != {user.Nickname} " +
                $"because Nickname == null => {user.Nickname == null}");
        }
        [Command("xp")]
        [Cooldown(5)]
        [Remarks("Get information about your level etc")]
        public async Task xp()
        {
            var user = (SocketGuildUser)Context.User;
            var account = UserAccounts.GetAccount(user);
            var embed = new EmbedBuilder();
            var p = new PlayerFighter(user);

            embed.WithColor(Colors.get(account.element.ToString()));
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
        public async Task status(SocketGuildUser user = null)
        {
            user = user ?? (SocketGuildUser) Context.User;
            var account = UserAccounts.GetAccount(user);
            var embed = new EmbedBuilder();
            var p = new PlayerFighter(user);

            embed.WithColor(Colors.get(account.element.ToString()));
            var author = new EmbedAuthorBuilder();
            author.WithName(user.DisplayName());
            author.WithIconUrl(user.GetAvatarUrl());
            embed.WithAuthor(author);
            embed.WithThumbnailUrl(user.GetAvatarUrl());
            //embed.WithDescription($"Status.");

            embed.AddField("Element", account.element, true);
            embed.AddField("Class", account.gsClass, true);

            embed.AddField("Level", account.LevelNumber, true);
            embed.AddField("Rank", UserAccounts.GetRank(user) + 1, true);

            embed.AddField("XP", account.XP, true);
            embed.AddField("XP to level up", Leveling.XPforNextLevel(account.XP), true);

            embed.AddField("Colosso wins", account.ColossoWins, true);
            //embed.AddField("", "");

            embed.AddField("Stats", p.stats.ToString());
            embed.AddField("Psynergy", p.getMoves());
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
        public async Task tri(SocketGuildUser user = null)
        {
            user = user ?? (SocketGuildUser)Context.User;
            var account = UserAccounts.GetAccount(user);
            var embed = new EmbedBuilder();
            var p = new PlayerFighter(user);

            embed.WithColor(Colors.get(account.element.ToString()));
            var author = new EmbedAuthorBuilder();
            author.WithName(user.DisplayName());
            author.WithIconUrl(user.GetAvatarUrl());
            embed.WithAuthor(author);
            embed.WithThumbnailUrl(user.GetAvatarUrl());
            //embed.WithDescription($"Status.");

            embed.AddField("Damage (Ninja)", account.damageDealt, true);
            embed.AddField("Kills By Hand (Samurai)", account.killsByHand, true);
            embed.AddField("HP Healed (White Mage)", account.HPhealed, true);
            embed.AddField("Revives (Medium)", account.revives, true);
            embed.AddField("Solos (Ranger)", account.soloBattles, true);
            embed.AddField("Teammates (Dragoon)", account.totalTeamMates, true);
            embed.AddField("Days Active (Hermit)", account.uniqueDaysActive, true);
            embed.AddField("Commands Used (Scrapper)", account.commandsUsed);
            embed.AddField("Wins/Streak (Brute, Curse Mage and More)", $"{account.ColossoWins}, {account.ColossoHighestStreak}");
            embed.AddField("Rps Wins/Streak (Aqua/Air Seer)", $"{account.rpsWins}, {account.rpsStreak}");
            embed.AddField("Channel Switches (Pilgrim)", account.channelSwitches);
            embed.AddField("Curse Mage (Written Curse, Quoted Matthew)", $"{account.hasWrittenCurse}, {account.hasQuotedMatthew}");

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
        public async Task roleInfo([Remainder] string args)
        {
            args = args.ToLower();
            var mentionedRole = Context.Guild.Roles.Where(r => r.Name.ToLower() == args).FirstOrDefault();
            if (mentionedRole == null)
            {
                mentionedRole = Context.Guild.Roles.Where(r => r.Name.ToLower().StartsWith(args)).FirstOrDefault();
            }
            if (mentionedRole == null || mentionedRole.IsEveryone) return;

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
        public async Task countUsers()
        {
            var count = Context.Guild.Users.Count;
            var offline = Context.Guild.Users.Where(u => u.Status == UserStatus.Offline).Count();
            var online = count - offline;
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.get("Iodem"));
            embed.WithDescription($"{count} Users with {online} Online");

            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("choose"), Alias("pick")]
        [Cooldown(15)]
        [Remarks("Choose from several words or group of words seperated by ','")]
        public async Task choose([Remainder] string s)
        {
            var choices = s.Split(' ');
            if (s.Contains(','))
            {
                choices = s.Split(',');
            }
            foreach(string c in choices)
            {
                c.Trim();
            }
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.get("Iodem"));
            var choice = choices[(new Random()).Next(0, choices.Length)];
            embed.WithDescription($"➡️ {choice}");
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }

        [Command("rank"), Alias("top")]
        [Cooldown(15)]
        [Remarks("Get the most active users and your rank")]
        public async Task rank()
        {
            var topAccounts = UserAccounts.GetTop(10);
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.get("Iodem"));
            string[] Emotes = new string[] { "🥇", "🥈", "🥈", "🥉", "🥉", "🥉", "   ", "   ", "   ", "   " };
            var builder = new StringBuilder();
            for (int i = 0; i < 10; i++)
            {
                var curAccount = topAccounts[i];
                builder.Append($"`{i+1}` {Emotes[i]} {curAccount.Name.PadRight(15)} - `Lv{curAccount.LevelNumber}` - `{curAccount.XP}xp` \n");
                //builder.Append($"`{i + 1}` {Emotes[i]} - `Lv{curAccount.LevelNumber}` - `{curAccount.XP}xp` \n");
            }

            var rank = UserAccounts.GetRank(Context.User);
            //Console.WriteLine(rank);
            var account = UserAccounts.GetAccount(Context.User);
            if (rank >= 10)
            {
                builder.Append("... \n");
                builder.Append($"`{rank+1}` {Context.User.Username.PadRight(15)} - `Lv{account.LevelNumber}` - `{account.XP}xp`");
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
            embed.WithColor(Colors.get("Iodem"));
            SocketGuildUser user;
            if(Context.Message.MentionedUsers.FirstOrDefault() != null)
            {
                user = (SocketGuildUser) Context.Message.MentionedUsers.FirstOrDefault();
            } else
            {
                user = (SocketGuildUser) Context.User;
            }

            var Role = Context.Guild.Roles.Where(r => r.Id == 511704880122036234).FirstOrDefault();
            if (Role == null) return;

            if(Role.Members.Where(m => m.Id == user.Id).FirstOrDefault() == null)
            {
                await user.AddRoleAsync(Role);
            } else
            {
                await user.RemoveRoleAsync(Role);
            }

            embed.WithDescription($"{user.Nickname} is a {Role.Name} now!");
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }
    }
}
    