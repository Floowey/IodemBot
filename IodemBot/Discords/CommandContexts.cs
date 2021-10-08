using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace IodemBot
{
    public interface IIodemCommandContext
    {
        public ISocketMessageChannel Channel { get; }
        public SocketMessage Message { get; }
        public IGuild Guild { get; }
        public SocketUser User { get; }
        public Task ReplyAsync(string message = null, Embed embed = null, MessageComponent component = null);

        public static IIodemCommandContext GetContext(object Context)
        {
            if (Context is SocketCommandContext c)
            {
                return new TextCommandContext(c);
            }
            throw new ArgumentException("Did not recognize Context.");
        }
    }

    public class TextCommandContext : IIodemCommandContext
    {
        public TextCommandContext(SocketCommandContext context)
        {
            this.context = context;
        }

        private SocketCommandContext context;

        public ISocketMessageChannel Channel => context.Channel;

        public SocketMessage Message => context.Message;

        public IGuild Guild => context.Guild;

        public SocketUser User => context.User;

        public async Task ReplyAsync(string text = null, Embed embed = null, MessageComponent component = null)
        {
            await Channel.SendMessageAsync(text: text, embed: embed, component:component);
        }
    }
}
