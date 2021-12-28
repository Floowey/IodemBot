using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IodemBot.ColossoBattles;
using IodemBot.Core;
using IodemBot.Discords.Services;
using IodemBot.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace IodemBot
{
    internal class Program
    {
        private static DiscordSocketClient _client;

        private readonly string[] _welcomeMsg =
        {
            "Welcome, {0}! Just ignore that strange tree out front!",
            "Welcome, {0}! We'll forget that whole curse business in no time!",
            "Welcome, {0}! You may enter, so long as you do not disrupt the peace.",
            "Welcome back, {0}! It's good to have you home!",
            "Shoot... What was my first line? \"Welcome, {0}, to the Palace of the Dragon King\"?",
            "Ah! {0}! Welcome, welcome... Listen, sorry about all that \"not letting you in\" business before... Don't take it personally.",
            "Welcome, welcome, {0}, step right up! Care for a round of Super Lucky Dice?",
            "{0} joins the party!",
            "Listen, this is {0}'s quest now... We're just doing what we can to help out...",
            "Well, I'll need to call you something. Hmm... You look like {0}.",
            "I want to scream. But {0} does not like it when I do that.",
            "Appearances can be an illusion... {0} has a caring heart.",
            "Isaac gave a Hard Nut to {0}",
            "Felix gave a Hard Nut to {0}",
            "You're {0}? The one they're all talking about? I heard rumors that you were a huge, hulking man. I guess they were wrong.",
            "Well, if it isn't {0}, too! Where do you all plan to go today?",
            "Hello {0}! Is 150 coins for your Nut good?"
        };

        private static void Main(string[] args)
        {
            try
            {
                Global.RunningSince = DateTime.Now;
                new Program().StartAsync().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                var date = DateTime.Now.ToString("yyyy_mm_dd");
                Console.WriteLine(e.ToString());
                File.AppendAllText($"Logs/{date}_crash.log", e.ToString());
            }
        }

        private async Task StartAsync()
        {
            if (string.IsNullOrEmpty(Config.Bot.Token)) return;
            await using var services = ConfigureServices();
            _client = services.GetRequiredService<DiscordSocketClient>();

            Global.Client = _client;

            _client.Log += Log;
            _client.Ready += Client_Ready;
            _client.UserLeft += Client_UserLeft;
            _client.UserJoined += Client_UserJoined;
            _client.UserBanned += _client_UserBanned;
            _client.GuildMemberUpdated += Client_GuildMemberUpdated;

            await _client.LoginAsync(TokenType.Bot, Config.Bot.Token);
            await _client.StartAsync();

            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();
            await services.GetRequiredService<ActionService>().InitializeAsync();
            services.GetRequiredService<RequestContextService>().Initialize();

            await services.GetRequiredService<MessageHandler>().InitializeAsync(_client);
            await Task.Delay(-1);
        }

        private async Task _client_UserBanned(SocketUser user, SocketGuild guild)
        {
            if (GuildSettings.GetGuildSettings(guild).SendLeaveMessage)
            {
                var r = await guild.GetBanAsync(user);
                var channel = GuildSettings.GetGuildSettings(guild).TestCommandChannel;
                _ = channel.SendMessageAsync($"{user.Mention} left the party :) {r.Reason}");
            }
            await Task.CompletedTask;
        }

        private async Task Client_GuildMemberUpdated(Cacheable<SocketGuildUser, ulong> before, SocketGuildUser user)
        {
            var guild = GuildSettings.GetGuildSettings(user.Guild);
            var userBefore = before.Value;
            if (userBefore.DisplayName() != user.DisplayName())
            {
                EntityConverter.ConvertUser(user).Name = user.DisplayName();
                _ = guild.TestCommandChannel
                    .SendMessageAsync(
                        $"{user.Mention} changed Nickname from {userBefore.DisplayName()} to {user.DisplayName()}");
            }

            Console.WriteLine($"Updated: before:{userBefore.IsPending} after:{user.IsPending}");
            if (userBefore.IsPending.Value == true && user.IsPending.Value == false)
                await SendWelcomeMessage(user);

            //if (userBefore.PremiumSince.HasValue != user.PremiumSince.HasValue)
            //{
            //    var isBoosting = user.PremiumSince.HasValue;
            //    if (isBoosting)
            //    {
            //        _ = guild.MainChannel
            //            .SendMessageAsync(
            //                $"<:Exclamatory:549529360604856323> {user.Mention} is now boosting the server.");
            //    }
            //    else
            //    {
            //        _ = guild.TestCommandChannel
            //        .SendMessageAsync(
            //            $"<:Exclamatory:549529360604856323> {user.Mention} is no longer boosting the server.");
            //    }
            //}

            await Task.CompletedTask;
        }

        private async Task SendWelcomeMessage(SocketGuildUser user)
        {
            var guild = GuildSettings.GetGuildSettings(user.Guild);
            if (GuildSettings.GetGuildSettings(user.Guild).SendWelcomeMessage)
                _ = guild.MainChannel.SendMessageAsync(embed:
                    new EmbedBuilder()
                        .WithColor(Colors.Get("Iodem"))
                        .WithDescription(string.Format(_welcomeMsg.Random(), user.DisplayName()))
                        .Build());
        }

        private async Task Client_UserJoined(SocketGuildUser user)
        {
            _ = GuildSettings.GetGuildSettings(user.Guild).TestCommandChannel.SendMessageAsync(embed:
                new EmbedBuilder()
                    .WithAuthor(user)
                    .AddField("Account Created", user.CreatedAt)
                    .AddField("User Joined", user.JoinedAt)
                    .AddField("Status", user.Status, true)
                    .Build());

            Console.WriteLine($"Joined: hasvalue:{user.IsPending.HasValue} after:{user.IsPending.Value}");
            if (user.IsPending.HasValue && !user.IsPending.Value)
            {
                await SendWelcomeMessage(user);
            }
            await Task.CompletedTask;
        }

        private async Task Client_UserLeft(SocketGuild guild, SocketUser user)
        {
            var settings = GuildSettings.GetGuildSettings(guild);
            if (settings.SendLeaveMessage)
            {
                var channel = settings.TestCommandChannel;
                _ = channel.SendMessageAsync($"{user.Mention} ({user.Username}) left the party :(");
            }
            await Task.CompletedTask;
        }

        private async Task Client_Ready()
        {
            _client.Ready -= Client_Ready;
            var channel = (SocketTextChannel)_client.GetChannel(535209634408169492) ??
                          (SocketTextChannel)_client.GetChannel(668443234292334612);
            if (channel != null && (DateTime.Now - Global.RunningSince).TotalSeconds < 15)
                await channel.SendMessageAsync(embed: new EmbedBuilder()
                    .WithTitle("Hello, I am back up.")
                    .AddField("OS", Environment.OSVersion.Platform, true)
                    .AddField("Latency", _client.Latency, true)
                    .AddField("Build Time", File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location), true)
                    .AddField("Version", Assembly.GetExecutingAssembly().GetName().Version, true)
                    .AddField("Prefix", Config.Bot.CmdPrefix, true)
                    .AddField("System Time", DateTime.Now, true)
                    .WithAuthor(_client.CurrentUser)
                    .WithColor(Colors.Get("Iodem"))
                    .Build());
            await _client.SetStatusAsync(UserStatus.Idle);
            Global.UpSince = DateTime.UtcNow;
        }

        private async Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.Message);
            var date = DateTime.Now.ToString("yyyy_MM_dd");
            try
            {
                File.AppendAllText($"Logs/{date}_log.log", msg.Message + Environment.NewLine);
            }
            catch
            {
            }

            try
            {
                if (msg.Exception != null)
                {
                    Console.WriteLine(msg.Exception.ToString());
                    File.AppendAllText($"Logs/{date}_log.log", msg.Exception + Environment.NewLine);
                }
            }
            catch
            {
                File.AppendAllText($"Logs/{date}_log.log", "Couldn't print Exception.\n");
            }

            await Task.CompletedTask;
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
                {
                    GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildPresences | GatewayIntents.GuildMembers,
                    AlwaysDownloadUsers = true,
                    LogLevel = LogSeverity.Info,
                    DefaultRetryMode = RetryMode.AlwaysRetry
                }))
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<ActionService>()
                .AddSingleton<MessageHandler>()
                .AddSingleton<ColossoBattleService>()
                .AddScoped<RequestContextService>()
                .BuildServiceProvider();
        }
    }
}