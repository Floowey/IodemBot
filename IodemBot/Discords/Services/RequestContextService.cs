using Discord.Commands;
using Discord.WebSocket;
using IodemBot.Discords.Contexts;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace IodemBot.Discords.Services
{
    public class RequestContextService
    {
        public RequestContext Context { get; set; }

        //DI
        private readonly IServiceProvider _services;

        public RequestContextService(IServiceProvider services)
        {
            _services = services;
        }

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
