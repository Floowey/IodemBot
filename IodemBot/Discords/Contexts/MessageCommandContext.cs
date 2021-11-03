using Discord.Commands;
using Discord.WebSocket;

namespace IodemBot.Discords.Contexts
{
    public class MessageCommandContext : SocketCommandContext
    {
        public string CommandInput { get; private set; }

        public MessageCommandContext(DiscordSocketClient client, SocketUserMessage msg, string input) : base(client, msg)
        {
            CommandInput = input;
        }
    }
}