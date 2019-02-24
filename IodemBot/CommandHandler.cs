using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using System;
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
            service = new CommandService();
            await service.AddModulesAsync(Assembly.GetEntryAssembly(), null);
            client.MessageReceived += HandleCommandAsync;
            Global.Client = client;
            //badWords = File.ReadAllLines("Resources/bad_words.txt");
        }

        private async Task HandleCommandAsync(SocketMessage s)
        {
            SocketUserMessage msg = s as SocketUserMessage;
            if (msg == null) return;
            var context = new SocketCommandContext(client, msg);
            if (context.User.IsBot) return;
            int argPos = 0;

            if (msg.HasStringPrefix(Config.bot.cmdPrefix, ref argPos) || msg.HasMentionPrefix(client.CurrentUser, ref argPos))
            {
                var result = await service.ExecuteAsync(context, argPos, null);
                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                {
                    Console.WriteLine(result.ErrorReason);
                }

                await ServerGames.UserSentCommand((SocketGuildUser) context.User, (SocketTextChannel) context.Channel);
            }
        }
    }
}