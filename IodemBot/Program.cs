using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using IodemBot.Modules.GoldenSunMechanics;
using System.IO;

namespace IodemBot
{
    class Program
    {
        private static DiscordSocketClient client;
        private static CommandHandler handler;
        private static MessageHandler msgHandler;

        static void Main(string[] args)
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
            if (string.IsNullOrEmpty(Config.bot.token)) return;

            var version = System.Environment.OSVersion.Version;
            if (version.Major == 6 && version.Minor == 1)
            {
                Console.WriteLine("Windows 7");
                client = new DiscordSocketClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Verbose,
                    WebSocketProvider = Discord.Net.Providers.WS4Net.WS4NetProvider.Instance
                });
            } else
            {
                Console.WriteLine("Not Windows 7");
                client = new DiscordSocketClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Verbose,
                    //WebSocketProvider = Discord.Net.Providers.WS4Net.WS4NetProvider.Instance
                });
            }

            client.Log += Log;
            client.ReactionAdded += Client_ReactionAdded;
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

        private async Task Client_UserJoined(SocketGuildUser user)
        {
            await user.AddRoleAsync(user.Guild.Roles.Where(r => r.Id == 355560889942016000).First());
        }

        private async Task Client_UserLeft(SocketGuildUser arg)
        {
            await Task.CompletedTask;
        }

        private async Task Client_Ready()
        {
            //setup colosso
            await client.SetGameAsync("in Babi's Palast.", "https://www.twitch.tv/directory/game/Golden%20Sun", ActivityType.Streaming);
            Global.UpSince = DateTime.UtcNow;
        }

        private async Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            await Modules.ColossoBattles.ColossoPvE.ReactionAdded(cache, channel, reaction);
        }

        private async Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.Message);
            var date = DateTime.Now.ToString("yyyy_MM_dd");
            File.AppendAllText($"Logs/{date}_log.log",msg.Message + "\n");
            try
            {
                if(msg.Exception != null)
                    File.AppendAllText($"Logs/{date}_log.log", msg.Exception.InnerException.ToString() + "\n");
            } catch
            {
                File.AppendAllText($"Logs/{date}_log.log", $"Couldn't print Exception.\n");
            }
            await Task.CompletedTask;
        }
    }
}