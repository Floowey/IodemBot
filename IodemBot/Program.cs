using Discord;
using Discord.WebSocket;
using IodemBot.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IodemBot
{
    internal class Program
    {
        private static DiscordSocketClient client;
        private static CommandHandler handler;
        private static MessageHandler msgHandler;

        private static void Main(string[] args)
        {
            try
            {
                new Program().StartAsync().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                var date = DateTime.Now.ToString("yyyy_mm_dd");
                File.AppendAllText($"Logs/{date}_crash.log", e.Message + "\n" + e.InnerException.ToString());
            }
        }

        public async Task StartAsync()
        {
            if (string.IsNullOrEmpty(Config.bot.token))
            {
                return;
            }

            var version = System.Environment.OSVersion.Version;
            if (version.Major == 6 && version.Minor == 1)
            {
                Console.WriteLine("Windows 7");
                client = new DiscordSocketClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Verbose,
                    WebSocketProvider = Discord.Net.Providers.WS4Net.WS4NetProvider.Instance
                });
            }
            else
            {
                Console.WriteLine("Not Windows 7");
                client = new DiscordSocketClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Verbose,
                    //WebSocketProvider = Discord.Net.Providers.WS4Net.WS4NetProvider.Instance
                });
            }

            Global.Client = client;

            client.Log += Log;
            client.Ready += Client_Ready;
            client.UserLeft += Client_UserLeft;
            client.UserJoined += Client_UserJoined;
            await client.LoginAsync(TokenType.Bot, Config.bot.token);
            await client.StartAsync();
            handler = new CommandHandler();
            await handler.InitializeAsync(client);
            msgHandler = new MessageHandler();
            await msgHandler.InitializeAsync(client);
            await Task.Delay(-1);
        }

        private string[] welcomeMsg = {
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
            var embed = new EmbedBuilder();
            embed.WithColor(Colors.Get("Iodem"));
            embed.WithDescription(String.Format(welcomeMsg[Global.Random.Next(0, welcomeMsg.Length)], user.DisplayName()));

            await user.AddRoleAsync(user.Guild.Roles.Where(r => r.Id == 355560889942016000).First());
            await ((SocketTextChannel)client.GetChannel(355558866282348575)).SendMessageAsync(embed: embed.Build());
        }

        private async Task Client_UserLeft(SocketGuildUser user)
        {
            await ((SocketTextChannel)client.GetChannel(506961678928314368)).SendMessageAsync($"{user.DisplayName()} left the party :(.");
        }

        private async Task Client_Ready()
        {
            //setup colosso
            await client.SetGameAsync("in Babi's Palast.", "https://www.twitch.tv/directory/game/Golden%20Sun", ActivityType.Streaming);
            Global.UpSince = DateTime.UtcNow;
        }

        private async Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.Message);
            var date = DateTime.Now.ToString("yyyy_MM_dd");
            File.AppendAllText($"Logs/{date}_log.log", msg.Message + "\n");
            try
            {
                if (msg.Exception != null)
                {
                    File.AppendAllText($"Logs/{date}_log.log", msg.Exception.InnerException.ToString() + "\n");
                }
            }
            catch
            {
                File.AppendAllText($"Logs/{date}_log.log", $"Couldn't print Exception.\n");
            }
            await Task.CompletedTask;
        }
    }
}