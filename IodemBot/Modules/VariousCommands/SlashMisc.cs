using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using IodemBot.Discords;
using IodemBot.Discords.Actions;
using IodemBot.Discords.Actions.Attributes;
using IodemBot.Discords.Services;
using IodemBot.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace IodemBot.Modules.VariousCommands
{
    public class PingCommand : IodemBotCommandAction
    {
        public override bool GuildsOnly => false;
        [ActionParameterComponent(Order = 1, Name = "p", Description = "d", Required = false)]
        public int pinged { get; set; } = 0;
        public override ActionGlobalSlashCommandProperties SlashCommandProperties => new()
        {
            Description = "pingping",
            Name = "pingping"
        };
        public override ActionCommandRefreshProperties CommandRefreshProperties => new()
        {
            FillParametersAsync = (selectOptions, idOptions) =>
            {
                if (idOptions != null && idOptions.Any())
                    pinged = Convert.ToInt32(idOptions[0]); 
                return Task.CompletedTask;
            },
            CanRefreshAsync = CanRefreshAsync,
            RefreshAsync = RefreshAsync
        };

        public Task<(bool, string)> CanRefreshAsync(bool intoNew) => Task.FromResult((true, (string)null));
        public Task RefreshAsync(bool intoNew, MessageProperties msgProps)
        {
            msgProps.Content = $"{pinged}";
            msgProps.Components = GetComponent();
            Console.WriteLine(pinged);
            return Task.CompletedTask;
        }

        public MessageComponent GetComponent()
        {
            var builder = new ComponentBuilder();
            string[] names = new[] { "You wan sum?!", "MORE", "FASTER", "HARDER", "STRONGER", "IS THAT ALL?", "ouch.", "stop it", "no", "MOOOOAAAR" };
            builder.WithButton(names.Random(), $"#{nameof(PingCommand)}.{pinged + 1}");

            return builder.Build() ;
        }

        public override List<ActionTextCommandProperties> TextCommandProperties => new()
        {
            new()
            {
                Name = "pingping",
                Aliases = new() { "pong" },
                Summary = "Ping Pong",
                ShowInHelp = true
            }
        };

        public override async Task RunAsync()
        {
            var cb = new ComponentBuilder();
            cb.WithButton("Pong", $"#{nameof(PingCommand)}.{pinged+1}");
            await Context.ReplyWithMessageAsync(EphemeralRule, $"Plong! {pinged}", components: cb.Build());
        }

        IServiceScope _scope;
        RequestContextService _requestContextService;

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var guildResult = IsGameCommandAllowedInGuild();
            if (!guildResult.Success)
                return Task.FromResult(guildResult);

            _scope = ServiceProvider.CreateScope();
            _requestContextService = _scope.ServiceProvider.GetRequiredService<RequestContextService>();
            _requestContextService.AddContext(Context);

            return Task.FromResult(guildResult);
        }
    }
    public class AddAllGuildCommands : IodemBotCommandAction
    {

        public override EphemeralRule EphemeralRule => EphemeralRule.EphemeralOrFail;

        public override List<ActionTextCommandProperties> TextCommandProperties => new()
        {
            new()
            {
                Name = "AddAllGuildCommands"
            }
        };

        public override async Task RunAsync()
        {
            var action = ServiceProvider.GetRequiredService<ActionService>();
            await action.AddAllGuildCommands(Context.Guild.Id);
            await Context.ReplyWithMessageAsync(EphemeralRule, message: "Hopefully did that.");
        }

        IServiceScope _scope;
        RequestContextService _requestContextService;

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var guildResult = IsGameCommandAllowedInGuild();
            if (!guildResult.Success)
                return Task.FromResult(guildResult);

            _scope = ServiceProvider.CreateScope();
            _requestContextService = _scope.ServiceProvider.GetRequiredService<RequestContextService>();
            _requestContextService.AddContext(Context);

            return Task.FromResult(guildResult);
        }
    }

    public class PokeUser : IodemBotCommandAction
    {

        [ActionParameterText(Order =1, Description = "User", IsRemainder = true, Name = "user", ParameterType=typeof(IUser))]
        [ActionParameterSlash(Order =1, Description = "User to poke", Name = "user", Required =true, Type=ApplicationCommandOptionType.User)]
        public IUser TargetUser { get; set; }
        public override bool GuildsOnly => false;
        public override ActionGlobalSlashCommandProperties SlashCommandProperties => new()
        {
            Description = "poke",
            Name = "poke",
            FillParametersAsync = options =>
            {
                if (options != null)
                    TargetUser = ValueForSlashOption<IUser>(options, nameof(TargetUser));

                return Task.CompletedTask;
            }
        };

        public override List<ActionTextCommandProperties> TextCommandProperties => new()
        {
            new()
            {
                Name = "poke",
                Summary = "Poke Someone",
                ShowInHelp = true,
                FillParametersAsync = options =>
                {
                    if (options != null)
                        TargetUser = options[0] == null ? null : options[0] as IUser;

                    return Task.CompletedTask;
                }
            }
        };

        public override ActionGlobalUserCommandProperties UserCommandProperties => new()
        {
            Name = "poke",
            FillParametersAsync = (user) =>
            {
                TargetUser = user;
                return Task.CompletedTask;
            }
        };

        public override async Task RunAsync()
        {
            
            TargetUser ??= Context.User;
            await Context.ReplyWithMessageAsync(EphemeralRule, $"Poked {TargetUser.Username}");
        }
    }
}
