using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IodemBot.Discords.Actions;
using IodemBot.Discords.Actions.Attributes;
using IodemBot.Discords.Contexts;
using IodemBot.Discords.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Discords
{
    public abstract class ActionRunFactory
    {
        public abstract Task RunActionAsync();

        public static ActionRunFactory Find(IServiceProvider services, RequestContext context, SocketInteraction interaction)
        {
            if (interaction is SocketSlashCommand slashCommand)
                return new ActionSlashRunFactory(services, context, slashCommand);
            else if (interaction is SocketMessageCommand msgCommand)
                return new ActionMessageRunFactory(services, context, msgCommand);
            else if (interaction is SocketUserCommand userCommand)
                return new ActionUserRunFactory(services, context, userCommand);
            else if (interaction is SocketMessageComponent component)
            {
                if (component.Data.CustomId.StartsWith('#') || component.Data.CustomId.StartsWith('^')) //# = refresh component, ^ = refresh into new component
                    return new ActionRefreshRunFactory(services, context, component);
                else
                    return new ActionComponentRunFactory(services, context, component);
            }

            return null;
        }

        public static ActionRunFactory Find(IServiceProvider services, RequestContext context, CommandInfo commandInfo, object[] parmValues) => new ActionTextRunFactory(services, context, commandInfo, parmValues);
    }


    public abstract class ActionRunFactory<TInteraction, TAction> : ActionRunFactory where TInteraction : class where TAction : BotAction
    {
        protected TInteraction _interaction;
        protected RequestContext _context;
        protected IServiceProvider _services;
        protected ActionService _actionService;

        public ActionRunFactory(IServiceProvider services, RequestContext context, TInteraction interaction)
        {
            _services = services;
            _context = context;
            _interaction = interaction;

            _actionService = _services.GetRequiredService<ActionService>();
        }

        protected abstract string InteractionNameForLog { get; }

        protected abstract TAction GetAction();
        protected abstract Task PopulateParametersAsync(TAction action);
        protected abstract Task RunActionAsync(TAction action);

        public override async Task RunActionAsync()
        {
            //using IDisposable typingObject = _context is RequestCommandContext ? _context.Channel?.EnterTypingState() : null;

            var action = GetAction();
            if (action == null)
                throw new CommandInvalidException();

            action.Initialize(_services, _context);

            if (_interaction is SocketInteraction si && _context is RequestInteractionContext ic)
                QueueDefer(action, si, ic);
            if (!await PopulateAndValidateParametersAsync(action))
                return;
            if (!await CheckPreconditionsAsync(action))
                return;

            RunAndHandleAction(action);
        }

        private void QueueDefer(TAction action, SocketInteraction si, RequestInteractionContext ic)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    double secondsToWait = 2.1d - (DateTime.UtcNow - si.CreatedAt.UtcDateTime).TotalSeconds;
                    if (secondsToWait > 2.1)
                        secondsToWait = 2.1;

                    if (secondsToWait > 0)
                        await Task.Delay((secondsToWait * 1000).IntLop(Math.Floor));

                    await ic.HadBeenAcknowledgedAsync(RequestAcknowledgeStatus.Acknowledged, async () => await si.DeferAsync(action.EphemeralRule.ToEphemeral()));
                }
                catch { }
            });
        }

        private async Task<bool> PopulateAndValidateParametersAsync(TAction action)
        {
            try
            {
                await PopulateParametersAsync(action);
                if (!action.ValidateParameters<ActionParameterSlashAttribute>())
                {
                    await _context.ReplyWithMessageAsync(EphemeralRule.EphemeralOrFallback, "Something went wrong - you didn't fill in a required option!").ConfigureAwait(false);
                    return false;
                }
            }
            catch (CommandParameterValidationException ce)
            {
                await _context.ReplyWithMessageAsync(EphemeralRule.EphemeralOrFallback, ce.Message);
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await _context.ReplyWithMessageAsync(EphemeralRule.EphemeralOrFallback, "I couldn't understand something you entered in!").ConfigureAwait(false);
                return false;
            }
            return true;
        }

        private async Task<bool> CheckPreconditionsAsync(TAction action)
        {
            var (Success, Message) = await action.CheckPreconditionsAsync();
            if (!Success)
            {
                await _context.ReplyWithMessageAsync(EphemeralRule.EphemeralOrFallback, Message ?? "Something went wrong with using this command!").ConfigureAwait(false);
                return false;
            }
            return true;
        }

        private void RunAndHandleAction(TAction action)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await RunActionAsync(action);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    await _context.ReplyWithMessageAsync(action.EphemeralRule, "Something went wrong!").ConfigureAwait(false);
                    return;
                }
            });
        }
    }

    public class ActionSlashRunFactory : ActionRunFactory<SocketSlashCommand, BotCommandAction>
    {
        protected override string InteractionNameForLog => _interaction.Data.Name;

        public ActionSlashRunFactory(IServiceProvider services, RequestContext context, SocketSlashCommand interaction) : base(services, context, interaction) { }

        protected override BotCommandAction GetAction() => _actionService.GetAll().OfType<BotCommandAction>().FirstOrDefault(a => a.SlashCommandProperties != null && a.SlashCommandProperties.Name == _interaction.Data.Name);

        protected override async Task PopulateParametersAsync(BotCommandAction action)
        {
            if (action.SlashCommandProperties.FillParametersAsync != null)
                await action.SlashCommandProperties.FillParametersAsync(_interaction.Data.Options);
        }

        protected override async Task RunActionAsync(BotCommandAction action)
        {
            await action.RunAsync();
        }
    }

    public class ActionMessageRunFactory : ActionRunFactory<SocketMessageCommand, BotCommandAction>
    {
        protected override string InteractionNameForLog => _interaction.Data.Name;

        public ActionMessageRunFactory(IServiceProvider services, RequestContext context, SocketMessageCommand interaction) : base(services, context, interaction) { }

        protected override BotCommandAction GetAction() => _actionService.GetAll().OfType<BotCommandAction>().FirstOrDefault(a => a.MessageCommandProperties != null && a.MessageCommandProperties.Name == _interaction.Data.Name);

        protected override async Task PopulateParametersAsync(BotCommandAction action)
        {
            if (action.MessageCommandProperties.FillParametersAsync != null)
                await action.MessageCommandProperties.FillParametersAsync(_interaction.Data.Message);
        }

        protected override async Task RunActionAsync(BotCommandAction action)
        {
            await action.RunAsync();
        }
    }

    public class ActionUserRunFactory : ActionRunFactory<SocketUserCommand, BotCommandAction>
    {
        protected override string InteractionNameForLog => _interaction.Data.Name;

        public ActionUserRunFactory(IServiceProvider services, RequestContext context, SocketUserCommand interaction) : base(services, context, interaction) { }

        protected override BotCommandAction GetAction() => _actionService.GetAll().OfType<BotCommandAction>().FirstOrDefault(a => a.UserCommandProperties != null && a.UserCommandProperties.Name == _interaction.Data.Name);

        protected override async Task PopulateParametersAsync(BotCommandAction action)
        {
            if (action.UserCommandProperties.FillParametersAsync != null)
                await action.UserCommandProperties.FillParametersAsync(_interaction.Data.Member);
        }

        protected override async Task RunActionAsync(BotCommandAction action)
        {
            await action.RunAsync();
        }
    }

    public class ActionRefreshRunFactory : ActionRunFactory<SocketMessageComponent, BotCommandAction>
    {
        readonly string _commandTypeName;
        readonly object[] _idOptions;
        readonly bool _intoNew = false;

        protected override string InteractionNameForLog => _interaction.Data.CustomId;

        public ActionRefreshRunFactory(IServiceProvider services, RequestContext context, SocketMessageComponent interaction) : base(services, context, interaction)
        {
            if (string.IsNullOrWhiteSpace(_interaction.Data.CustomId))
                throw new CommandInvalidException();

            //# = refresh component, ^ = refresh into new component
            _intoNew = interaction.Data.CustomId.First() == '^';

            var splitId = interaction.Data.CustomId[1..].Split('.');
            _commandTypeName = splitId[0];
            _idOptions = splitId.Skip(1).Cast<object>().ToArray();
        }

        protected override BotCommandAction GetAction() => _actionService.GetAll().OfType<BotCommandAction>().FirstOrDefault(a => a.CommandRefreshProperties != null && a.GetType().Name == _commandTypeName);

        protected override async Task PopulateParametersAsync(BotCommandAction action)
        {
            if (action.CommandRefreshProperties.FillParametersAsync != null)
            {
                var selectOptions = _interaction.Data.Values?.ToArray();
                await action.CommandRefreshProperties.FillParametersAsync(selectOptions, _idOptions);
            }
        }

        protected override async Task RunActionAsync(BotCommandAction action)
        {
            var (Success, Message) = await action.CommandRefreshProperties.CanRefreshAsync(_intoNew);
            if (!Success)
            {
                await _context.ReplyWithMessageAsync(true, Message);
                return;
            }

            if (_intoNew)
            {
                var props = new MessageProperties();
                await action.CommandRefreshProperties.RefreshAsync(_intoNew, props);
                await _context.ReplyWithMessageAsync(action.EphemeralRule.ToEphemeral() ? EphemeralRule.EphemeralOrFail : EphemeralRule.Permanent, props.Content.GetValueOrDefault(), embed: props.Embed.GetValueOrDefault(), embeds: props.Embeds.GetValueOrDefault(),
                    allowedMentions: props.AllowedMentions.GetValueOrDefault(), components: props.Components.GetValueOrDefault());
            }
            else
            {
                await _context.UpdateReplyAsync(msgProps => action.CommandRefreshProperties.RefreshAsync(_intoNew, msgProps).GetAwaiter().GetResult());
            }
        }
    }

    public class ActionComponentRunFactory : ActionRunFactory<SocketMessageComponent, BotComponentAction>
    {
        readonly string _commandTypeName;
        readonly object[] _idOptions;

        protected override string InteractionNameForLog => _interaction.Data.CustomId;

        public ActionComponentRunFactory(IServiceProvider services, RequestContext context, SocketMessageComponent interaction) : base(services, context, interaction)
        {
            if (string.IsNullOrWhiteSpace(_interaction.Data.CustomId))
                throw new CommandInvalidException();

            //# = refresh component, ^ = refresh into new component
            var splitId = _interaction.Data.CustomId.Split('.');
            _commandTypeName = splitId[0];
            _idOptions = splitId.Skip(1).Cast<object>().ToArray();
        }

        protected override BotComponentAction GetAction() => _actionService.GetAll().OfType<BotComponentAction>().FirstOrDefault(a => a.GetType().Name == _commandTypeName);

        protected override async Task PopulateParametersAsync(BotComponentAction action)
        {
            var selectOptions = _interaction.Data.Values?.ToArray();
            await action.FillParametersAsync(selectOptions, _idOptions);
        }

        protected override async Task RunActionAsync(BotComponentAction action)
        {
            await action.RunAsync();
        }
    }

    public class ActionTextRunFactory : ActionRunFactory<CommandInfo, BotCommandAction>
    {
        readonly object[] _parmValues;
        ActionTextCommandProperties _textProperties;
        protected override string InteractionNameForLog => _interaction.Name;

        public ActionTextRunFactory(IServiceProvider services, RequestContext context, CommandInfo commandInfo, object[] parmValues) : base(services, context, commandInfo) { _parmValues = parmValues; }

        protected override BotCommandAction GetAction()
        {
            var action = _actionService.GetAll().OfType<BotCommandAction>().FirstOrDefault(s => s.TextCommandProperties != null && s.TextCommandProperties.Any(t => t.Name == _interaction.Name));

            _textProperties = action.TextCommandProperties.FirstOrDefault(t => t.Name == _interaction.Name);
            if (_textProperties == null)
                throw new CommandInvalidException();

            return action;
        }

        protected override async Task PopulateParametersAsync(BotCommandAction action)
        {
            if (_textProperties.FillParametersAsync != null)
                await _textProperties.FillParametersAsync(_parmValues);
        }

        protected override async Task RunActionAsync(BotCommandAction action)
        {
            await action.RunAsync();
        }
    }
}
