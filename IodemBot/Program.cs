using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IodemBot.Core;
using IodemBot.Discords.Services;
using IodemBot.Extensions;
using IodemBot.Modules.ColossoBattles;
using Microsoft.Extensions.DependencyInjection;

namespace IodemBot
{
    internal class Program
    {
        private static DiscordSocketClient client;

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

        public async Task StartAsync()
        {

            if (string.IsNullOrEmpty(Config.bot.token))
            {
                return;
            }
            using var services = ConfigureServices();
            client = services.GetRequiredService<DiscordSocketClient>();

            Global.Client = client;

            client.Log += Log;
            client.Ready += Client_Ready;
            client.UserLeft += Client_UserLeft;
            client.UserJoined += Client_UserJoined;
            client.GuildMemberUpdated += Client_GuildMemberUpdated;
            
            await client.LoginAsync(TokenType.Bot, Config.bot.token);
            await client.StartAsync();

            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();
            await services.GetRequiredService<ActionService>().InitializeAsync();
            services.GetRequiredService<RequestContextService>().Initialize();

            await services.GetRequiredService<MessageHandler>().InitializeAsync(client);
            await Task.Delay(-1);
        }

        private async Task Client_GuildMemberUpdated(Cacheable<SocketGuildUser,ulong> before, SocketGuildUser after)
        {
            if (before.HasValue && before.Value.DisplayName() != after.DisplayName())
            {
                
                EntityConverter.ConvertUser(after).Name = after.DisplayName();
                _ = GuildSettings.GetGuildSettings(after.Guild).TestCommandChannel
                    .SendMessageAsync($"{after.Mention} changed Nickname from {before.Value.DisplayName()} to {after.DisplayName()}");
            }
            await Task.CompletedTask;
        }

        private readonly string[] welcomeMsg = {
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
            "Hello {0}! Is 150 coins for your Nut good?",
        };

        private async Task Client_UserJoined(SocketGuildUser user)
        {
            if (GuildSettings.GetGuildSettings(user.Guild).sendWelcomeMessage)
            {
                _ = GuildSettings.GetGuildSettings(user.Guild).MainChannel.SendMessageAsync(embed:
                    new EmbedBuilder()
                    .WithColor(Colors.Get("Iodem"))
                    .WithDescription(string.Format(welcomeMsg.Random(), user.DisplayName()))
                    .Build());
            }

            _ = GuildSettings.GetGuildSettings(user.Guild).TestCommandChannel.SendMessageAsync(embed:
                new EmbedBuilder()
                .WithAuthor(user)
                .AddField("Account Created", user.CreatedAt)
                .AddField("User Joined", user.JoinedAt)
                .AddField("Status", user.Status, true)
                .Build());
            await Task.CompletedTask;
        }

        private async Task Client_UserLeft(SocketGuildUser user)
        {
            if (GuildSettings.GetGuildSettings(user.Guild).sendLeaveMessage)
            {
                _ = GuildSettings.GetGuildSettings(user.Guild).TestCommandChannel.SendMessageAsync($"{user.DisplayName()} left the party :(.");
            }
            await Task.CompletedTask;
        }

        private async Task Client_Ready()
        {
            client.Ready -= Client_Ready;
            var channel = (SocketTextChannel)client.GetChannel(535209634408169492) ?? (SocketTextChannel)client.GetChannel(668443234292334612);
            if (channel != null && (DateTime.Now - Global.RunningSince).TotalSeconds < 15)
            {
                await channel.SendMessageAsync($"Hello, I am back up.\nOS: {Environment.OSVersion}\nBuild Time: {File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location)}");
                foreach (var guild in client.Guilds)
                {
                    var gs = GuildSettings.GetGuildSettings(guild);
                    if (gs.AutoSetup && gs.ColossoChannel != null)
                    {
                        await ColossoCommands.Setup(guild);
                        Console.WriteLine($"Setup in {gs.Name}");
                    }
                }
            }
            //setup colosso
            await client.SetStatusAsync(UserStatus.Idle);
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
            catch { }
            try
            {
                if (msg.Exception != null)
                {
                    Console.WriteLine(msg.Exception.ToString());
                    File.AppendAllText($"Logs/{date}_log.log", msg.Exception.ToString() + Environment.NewLine);
                }
            }
            catch
            {
                File.AppendAllText($"Logs/{date}_log.log", $"Couldn't print Exception.\n");
            }
            await Task.CompletedTask;
        }
        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig()
                {
                    //AlwaysAcknowledgeInteractions = false,
                    GatewayIntents = GatewayIntents.All,
                    //AlwaysDownloadUsers = true,
                    LogLevel = LogSeverity.Info,
                    //MessageCacheSize = 10,
                    DefaultRetryMode = RetryMode.AlwaysRetry
                }))
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<ActionService>()
                .AddSingleton<MessageHandler>()
                .AddScoped<RequestContextService>()
                .BuildServiceProvider();
        }
    }

}