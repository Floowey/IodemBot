using Discord;
using Discord.Commands.Builders;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IodemBot.Discords.Actions
{ 
    public interface IActionUserCommandProperties
    {
        string Name { get; set; }
        Func<SocketUser, Task> FillParametersAsync { get; set; }
    }

    public class ActionGlobalUserCommandProperties : IActionUserCommandProperties
    {
        public string Name { get; set; }
        public Func<SocketUser, Task> FillParametersAsync { get; set; }
    }

    public class ActionGuildUserCommandProperties : IActionUserCommandProperties
    {
        public string Name { get; set; }
        public Func<SocketUser, Task> FillParametersAsync { get; set; }
    }
}
