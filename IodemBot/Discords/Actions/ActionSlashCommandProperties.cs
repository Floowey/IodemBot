using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace IodemBot.Discords.Actions
{
    public interface IActionSlashCommandProperties
    {
        string Name { get; set; }
        string Description { get; set; }
        Func<IEnumerable<SocketSlashCommandDataOption>, Task> FillParametersAsync { get; set; }
    }

    public class ActionGlobalSlashCommandProperties : IActionSlashCommandProperties
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Func<IEnumerable<SocketSlashCommandDataOption>, Task> FillParametersAsync { get; set; }
    }

    public class ActionGuildSlashCommandProperties : IActionSlashCommandProperties
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Func<IEnumerable<SocketSlashCommandDataOption>, Task> FillParametersAsync { get; set; }
    }
}