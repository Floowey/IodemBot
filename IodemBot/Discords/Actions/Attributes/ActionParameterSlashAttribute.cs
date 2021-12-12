using System;
using Discord;

namespace IodemBot.Discords.Actions.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ActionParameterSlashAttribute : Attribute, IActionParameterAttribute
    {
        public bool DefaultSubCommand { get; set; }
        public ApplicationCommandOptionType Type { get; set; }
        public string[] ParentNames { get; set; }
        public string[] FilterCommandNames { get; set; }
        public int Order { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public bool AutoComplete { get; set; }
        public bool Required { get; set; } = false;
    }
}