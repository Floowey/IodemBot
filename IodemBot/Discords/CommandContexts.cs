using System;
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

        public static IIodemCommandContext GetContext(object context)
        {
            if (context is SocketCommandContext c) return new TextCommandContext(c);
            throw new ArgumentException("Did not recognize Context.");
        }
    }

    public class TextCommandContext : IIodemCommandContext
    {
        private readonly SocketCommandContext _context;

        public TextCommandContext(SocketCommandContext context)
        {
            _context = context;
        }

        public ISocketMessageChannel Channel => _context.Channel;

        public SocketMessage Message => _context.Message;

        public IGuild Guild => _context.Guild;

        public SocketUser User => _context.User;

        public async Task ReplyAsync(string text = null, Embed embed = null, MessageComponent component = null)
        {
            await Channel.SendMessageAsync(text, embed: embed, component: component);
        }
    }
}