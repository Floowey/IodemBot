using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using IodemBot.Core.Leveling;

namespace IodemBot
{
    public class CommandHandler
    {
        private DiscordSocketClient client;
        private CommandService service;

        public async Task InitializeAsync(DiscordSocketClient client)
        {
            this.client = client;
            service = new CommandService(new CommandServiceConfig() {DefaultRunMode = RunMode.Async});
            await service.AddModulesAsync(Assembly.GetEntryAssembly(), null);
            client.MessageReceived += HandleCommandAsync;
            Global.Client = client;
        }

        private async Task HandleCommandAsync(SocketMessage s)
        {
            if (!(s is SocketUserMessage msg))
            {
                return;
            }

            var context = new SocketCommandContext(client, msg);
            if (context.User.IsBot)
            {
                return;
            }

            int argPos = 0;

            if (msg.HasStringPrefix(Config.bot.cmdPrefix, ref argPos, StringComparison.InvariantCultureIgnoreCase) || msg.HasMentionPrefix(client.CurrentUser, ref argPos))
            {
                var result = await service.ExecuteAsync(context, argPos, null);
                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                {
                    if (result is ExecuteResult execResult)
                    {
                        Console.WriteLine(execResult.Exception);
                    }
                    Console.WriteLine(result.ErrorReason);
                }
                _ = ServerGames.UserSentCommand(context.User, context.Channel);
            }
        }
    }
}