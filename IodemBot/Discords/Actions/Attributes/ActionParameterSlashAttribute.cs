using System;
using Discord;

namespace IodemBot.Discords.Actions.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ActionParameterSlashAttribute : Attribute, IActionParameterAttribute
    {
        public string[] FilterCommandNames { get; set; }
        public int Order { get; set; }

        public string Name { get; set; }
        public bool DefaultSubCommand { get; set; }
        public ApplicationCommandOptionType Type { get; set; }
        public string Description { get; set; }
        public bool Required { get; set; } = false;
        public string[] ParentNames { get; set; }
    }
}