using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using IodemBot.Core.UserManagement;
using IodemBot.Discords;
using IodemBot.Discords.Actions;
using IodemBot.Discords.Actions.Attributes;
using IodemBot.Discords.Contexts;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.Modules
{
    class DjinnInfoAction: IodemBotCommandAction
    {
        public override bool GuildsOnly => false;
        [ActionParameterSlash(Order=1, Name="djinn", Description= "Name of the djinn to be displayed", Required=true, Type=ApplicationCommandOptionType.String)]
        public string IDDjinn { get; set; }
        public override async Task RunAsync()
        {
            await Context.ReplyWithMessageAsync(EphemeralRule, embed: DjinnInfoEmbed());
        }
        public override ActionGuildSlashCommandProperties SlashCommandProperties => new ActionGuildSlashCommandProperties(){

            Name = "djinninfo",
            Description = "Give info on that one djinn you found but can't remember the name of.",
            FillParametersAsync =  options =>
            {
                if(options != null && options.Any())
                {
                    IDDjinn = (string)options.FirstOrDefault().Value;
                }
                return Task.CompletedTask;
            }
        };
        public override EphemeralRule EphemeralRule => EphemeralRule.Permanent;
        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            
            if(!DjinnAndSummonsDatabase.TryGetDjinn(IDDjinn, out Djinn djinn))
                return Task.FromResult((false, ":x: There is no such spirit with that description!"));
            return Task.FromResult((true, "Task failed successfully"));
            
        }
        private Embed DjinnInfoEmbed(){
            var djinn = DjinnAndSummonsDatabase.GetDjinn(IDDjinn);
            var embed = new EmbedBuilder();
            embed.WithAuthor($"{djinn.Name}");
            embed.AddField("Icon", djinn.Emote, true);
            embed.AddField("Stats", djinn.Stats.NonZerosToString(), true);
            var effectList = djinn.Effects.Count > 0 ? string.Join("\n", djinn.Effects.Select(e => e.ToString())) : "";
            embed.AddField("Description", string.Join("\n", djinn.ToString(), effectList, djinn.HasPriority ? "Always goes first." : ""));

            embed.WithColor(Colors.Get(djinn.Element.ToString()));

            return embed.Build();
        }
    }

}