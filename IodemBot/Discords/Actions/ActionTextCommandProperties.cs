using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands.Builders;

namespace IodemBot.Discords.Actions
{
    public class ActionTextCommandProperties
    {
        public string Name { get; set; }
        public List<string> Aliases { get; set; }
        public string Summary { get; set; }
        public bool ShowInHelp { get; set; } = false;
        public int? Priority { get; set; }
        public Func<object[], Task> FillParametersAsync { get; set; }
        public Func<IServiceProvider, CommandBuilder, Task> ModifyBuilder { get; set; }
    }
}