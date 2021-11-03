using Discord.Commands;
using Discord.WebSocket;

namespace IodemBot.Discords.Contexts
{
    public class MessageCommandContext : SocketCommandContext
    {
        public MessageCommandContext(DiscordSocketClient client, SocketUserMessage msg, string input)
            : base(client, msg)
        {
            CommandInput = input;
        }

        public string CommandInput { get; }
    }
}