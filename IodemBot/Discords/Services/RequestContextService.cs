using System;
using Discord.Commands;
using Discord.WebSocket;
using IodemBot.Discords.Contexts;

namespace IodemBot.Discords.Services
{
    public class RequestContextService
    {
        //DI
        private readonly IServiceProvider _services;

        public RequestContextService(IServiceProvider services)
        {
            _services = services;
        }

        public RequestContext Context { get; set; }

        public void Initialize()
        {
        }

        public void AddContext(RequestContext context)
        {
            Context = context;
        }

        public void AddContext(SocketCommandContext context)
        {
            Context = new RequestCommandContext(context);
        }

        public void AddContext(SocketSlashCommand interaction, DiscordSocketClient client)
        {
            Context = new RequestInteractionContext(interaction, client);
        }
    }
}