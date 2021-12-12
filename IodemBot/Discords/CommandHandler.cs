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
        private DiscordSocketClient _client;
        private CommandService _service;

        public async Task InitializeAsync(DiscordSocketClient client)
        {
            _client = client;
            _service = new CommandService();
            await _service.AddModulesAsync(Assembly.GetEntryAssembly(), null);
            client.MessageReceived += HandleCommandAsync;
            Global.Client = client; //#0094ff
        }

        private async Task HandleCommandAsync(SocketMessage s)
        {
            if (s is not SocketUserMessage msg) return;

            var context = new SocketCommandContext(_client, msg);
            if (context.User.IsBot) return;

            var argPos = 0;

            if (msg.HasStringPrefix(Config.Bot.CmdPrefix, ref argPos, StringComparison.InvariantCultureIgnoreCase) ||
                msg.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                var result = await _service.ExecuteAsync(context, argPos, null);
                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                {
                    if (result is ExecuteResult execResult) Console.WriteLine(execResult.Exception);
                    Console.WriteLine(result.ErrorReason);
                }

                _ = ServerGames.UserSentCommand(context.User, context.Channel);
            }
        }
    }
}