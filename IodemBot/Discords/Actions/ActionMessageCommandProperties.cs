using System;
using System.Threading.Tasks;
using Discord.WebSocket;

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