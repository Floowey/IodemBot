using Discord;
using Discord.Commands.Builders;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IodemBot.Discords.Actions 
{
    public interface IActionMessageCommandProperties
    {
        string Name { get; set; }
        Func<SocketMessage, Task> FillParametersAsync { get; set; }
    }

    public class ActionGlobalMessageCommandProperties : IActionMessageCommandProperties
    {
        public string Name { get; set; }
        public Func<SocketMessage, Task> FillParametersAsync { get; set; }
    }

    public class ActionGuildMessageCommandProperties : IActionMessageCommandProperties
    {
        public string Name { get; set; }
        public Func<SocketMessage, Task> FillParametersAsync { get; set; }
    }
}
