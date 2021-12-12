using System;
using System.Threading.Tasks;
using Discord.WebSocket;

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