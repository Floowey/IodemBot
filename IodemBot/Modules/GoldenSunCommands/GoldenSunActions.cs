using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IodemBot.Discords.Actions;
using IodemBot.Discords.Actions.Attributes;
using Discord;

namespace IodemBot.Modules
{
    public class ChangeElementAction : IodemBotCommandAction
    {
        [ActionParameterSlash(Order =1, Name ="element",Description ="el", Required =true, Type=ApplicationCommandOptionType.String)]
        [ActionParameterOptionString(Name = "Venus", Order=1, Value ="Venus")]
        [ActionParameterOptionString(Name = "Mars", Order=1, Value ="Mars")]
        [ActionParameterOptionString(Name = "Windy Boi", Order=3, Value ="Jupiter")]
        [ActionParameterOptionString(Name = "Mercury", Order=4, Value ="Mercury")]
        [ActionParameterOptionString(Name = "Exathi", Order = 0, Value = "none")]
        public Element SelectedElement { get; set; }
        public override ActionGuildSlashCommandProperties SlashCommandProperties => new()
        {
            Name = "element",
            Description = "Change your element",
            FillParametersAsync = options =>
            {
                if (options != null)
                    SelectedElement = Enum.Parse<Element>((string)options.FirstOrDefault().Value);
                
                return Task.CompletedTask;
            }
        };
        public override async Task RunAsync()
        {   
            await Context.ReplyWithMessageAsync(EphemeralRule, $"Chose Element: {SelectedElement}");
        }
    }
}
