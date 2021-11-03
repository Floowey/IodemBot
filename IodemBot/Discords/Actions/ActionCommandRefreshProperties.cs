using System;
using System.Threading.Tasks;
using Discord;

namespace IodemBot.Discords.Actions
{
    public class ActionCommandRefreshProperties
    {
        public Func<string[], object[], Task> FillParametersAsync { get; set; }
        public Func<bool, Task<(bool, string)>> CanRefreshAsync { get; set; }
        public Func<bool, MessageProperties, Task> RefreshAsync { get; set; }
    }
}