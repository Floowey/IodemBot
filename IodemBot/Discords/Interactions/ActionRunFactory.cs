using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IodemBot.Discords.Actions;
using IodemBot.Discords.Actions.Attributes;
using IodemBot.Discords.Contexts;
using IodemBot.Discords.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IodemBot.Discords
{
    public abstract class ActionRunFactory
    {
        public abstract Task RunActionAsync();

        public static ActionRunFactory Find(IServiceProvider services, RequestContext context,
            SocketInteraction interaction)
        {
            if (interaction is SocketSlashCommand slashCommand)
                return new ActionSlashRunFactory(services, context, slashCommand);

            if (interaction is SocketMessageCommand msgCommand)
                return new ActionMessageRunFactory(services, context, msgCommand);

            if (interaction is SocketUserCommand userCommand)
                return new ActionUserRunFactory(services, context, userCommand);

            if (interaction is SocketMessageComponent component)
            {
                if (component.Data.CustomId.StartsWith('#') ||
                    component.Data.CustomId.StartsWith('^')) //# = refresh component, ^ = refresh into new component
                    return new ActionRefreshRunFactory(services, context, component);
                return new ActionComponentRunFactory(services, context, component);
            }

            return null;
        }

        public static ActionRunFactory Find(IServiceProvider services, RequestContext context, CommandInfo commandInfo,
            object[] parmValues)
        {
            return new ActionTextRunFactory(services, context, commandInfo, parmValues);
        }
    }

    public abstract class ActionRunFactory<TInteraction, TAction> : ActionRunFactory
        where TInteraction : class where TAction : BotAction
    {
        protected ActionService ActionService;
        protected RequestContext Context;
        protected TInteraction Interaction;
        protected IServiceProvider Services;

        public ActionRunFactory(IServiceProvider services, RequestContext context, TInteraction interaction)
        {
            Services = services;
            Context = context;
            Interaction = interaction;

            ActionService = Services.GetRequiredService<ActionService>();
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

            action.Initialize(Services, Context);

            if (Interaction is SocketInteraction si && Context is RequestInteractionContext ic)
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
                    var secondsToWait = 2.1d - (DateTime.UtcNow - si.CreatedAt.UtcDateTime).TotalSeconds;
                    if (secondsToWait > 2.1)
                        secondsToWait = 2.1;

                    if (secondsToWait > 0)
                        await Task.Delay((secondsToWait * 1000).IntLop(Math.Floor));

                    await ic.HadBeenAcknowledgedAsync(RequestAcknowledgeStatus.Acknowledged,
                        async () => await si.DeferAsync(action.EphemeralRule.ToEphemeral()));
                }
                catch
                {
                }
            });
        }

        private async Task<bool> PopulateAndValidateParametersAsync(TAction action)
        {
            try
            {
                await PopulateParametersAsync(action);
                if (!action.ValidateParameters<ActionParameterSlashAttribute>())
                {
                    await Context.ReplyWithMessageAsync(EphemeralRule.EphemeralOrFallback,
                        "Something went wrong - you didn't fill in a required option!").ConfigureAwait(false);
                    return false;
                }
            }
            catch (CommandParameterValidationException ce)
            {
                await Context.ReplyWithMessageAsync(EphemeralRule.EphemeralOrFallback, ce.Message);
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await Context.ReplyWithMessageAsync(EphemeralRule.EphemeralOrFallback,
                    "I couldn't understand something you entered in!").ConfigureAwait(false);
                return false;
            }

            return true;
        }

        private async Task<bool> CheckPreconditionsAsync(TAction action)
        {
            var (success, message) = await action.CheckPreconditionsAsync();
            if (!success)
            {
                await Context.ReplyWithMessageAsync(EphemeralRule.EphemeralOrFallback,
                    message ?? "Something went wrong with using this command!").ConfigureAwait(false);
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
                    Console.WriteLine($"{action.Context.User} used {InteractionNameForLog}");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    await Context.ReplyWithMessageAsync(action.EphemeralRule, "Something went wrong!")
                        .ConfigureAwait(false);
                }
            });
        }
    }

    public class ActionSlashRunFactory : ActionRunFactory<SocketSlashCommand, BotCommandAction>
    {
        public ActionSlashRunFactory(IServiceProvider services, RequestContext context, SocketSlashCommand interaction)
            : base(services, context, interaction)
        {
        }

        protected override string InteractionNameForLog => Interaction.Data.Name;

        protected override BotCommandAction GetAction()
        {
            return ActionService.GetAll().OfType<BotCommandAction>().FirstOrDefault(a =>
                a.SlashCommandProperties != null && a.SlashCommandProperties.Name == Interaction.Data.Name);
        }

        protected override async Task PopulateParametersAsync(BotCommandAction action)
        {
            if (action.SlashCommandProperties.FillParametersAsync != null)
                await action.SlashCommandProperties.FillParametersAsync(Interaction.Data.Options);
        }

        protected override async Task RunActionAsync(BotCommandAction action)
        {
            await action.RunAsync();
        }
    }

    public class ActionMessageRunFactory : ActionRunFactory<SocketMessageCommand, BotCommandAction>
    {
        public ActionMessageRunFactory(IServiceProvider services, RequestContext context,
            SocketMessageCommand interaction) : base(services, context, interaction)
        {
        }

        protected override string InteractionNameForLog => Interaction.Data.Name;

        protected override BotCommandAction GetAction()
        {
            return ActionService.GetAll().OfType<BotCommandAction>().FirstOrDefault(a =>
                a.MessageCommandProperties != null && a.MessageCommandProperties.Name == Interaction.Data.Name);
        }

        protected override async Task PopulateParametersAsync(BotCommandAction action)
        {
            if (action.MessageCommandProperties.FillParametersAsync != null)
                await action.MessageCommandProperties.FillParametersAsync(Interaction.Data.Message);
        }

        protected override async Task RunActionAsync(BotCommandAction action)
        {
            await action.RunAsync();
        }
    }

    public class ActionUserRunFactory : ActionRunFactory<SocketUserCommand, BotCommandAction>
    {
        public ActionUserRunFactory(IServiceProvider services, RequestContext context, SocketUserCommand interaction) :
            base(services, context, interaction)
        {
        }

        protected override string InteractionNameForLog => Interaction.Data.Name;

        protected override BotCommandAction GetAction()
        {
            return ActionService.GetAll().OfType<BotCommandAction>().FirstOrDefault(a =>
                a.UserCommandProperties != null && a.UserCommandProperties.Name == Interaction.Data.Name);
        }

        protected override async Task PopulateParametersAsync(BotCommandAction action)
        {
            if (action.UserCommandProperties.FillParametersAsync != null)
                await action.UserCommandProperties.FillParametersAsync(Interaction.Data.Member);
        }

        protected override async Task RunActionAsync(BotCommandAction action)
        {
            await action.RunAsync();
        }
    }

    public class ActionRefreshRunFactory : ActionRunFactory<SocketMessageComponent, BotCommandAction>
    {
        private readonly string _commandTypeName;
        private readonly object[] _idOptions;
        private readonly bool _intoNew;

        public ActionRefreshRunFactory(IServiceProvider services, RequestContext context,
            SocketMessageComponent interaction) : base(services, context, interaction)
        {
            if (string.IsNullOrWhiteSpace(Interaction.Data.CustomId))
                throw new CommandInvalidException();

            //# = refresh component, ^ = refresh into new component
            _intoNew = interaction.Data.CustomId.First() == '^';

            var splitId = interaction.Data.CustomId[1..].Split('.');
            _commandTypeName = splitId[0];
            _idOptions = splitId.Skip(1).Cast<object>().ToArray();
        }

        protected override string InteractionNameForLog => Interaction.Data.CustomId;

        protected override BotCommandAction GetAction()
        {
            return ActionService.GetAll().OfType<BotCommandAction>().FirstOrDefault(a =>
                a.CommandRefreshProperties != null && a.GetType().Name == _commandTypeName);
        }

        protected override async Task PopulateParametersAsync(BotCommandAction action)
        {
            if (action.CommandRefreshProperties.FillParametersAsync != null)
            {
                var selectOptions = Interaction.Data.Values?.ToArray();
                await action.CommandRefreshProperties.FillParametersAsync(selectOptions, _idOptions);
            }
        }

        protected override async Task RunActionAsync(BotCommandAction action)
        {
            var (success, message) = await action.CommandRefreshProperties.CanRefreshAsync(_intoNew);
            if (!success)
            {
                await Context.ReplyWithMessageAsync(true, message);
                return;
            }

            if (_intoNew)
            {
                var props = new MessageProperties();
                await action.CommandRefreshProperties.RefreshAsync(_intoNew, props);
                await Context.ReplyWithMessageAsync(
                    action.EphemeralRule.ToEphemeral() ? EphemeralRule.EphemeralOrFail : EphemeralRule.Permanent,
                    props.Content.GetValueOrDefault(), embed: props.Embed.GetValueOrDefault(),
                    embeds: props.Embeds.GetValueOrDefault(),
                    allowedMentions: props.AllowedMentions.GetValueOrDefault(),
                    components: props.Components.GetValueOrDefault());
            }
            else
            {
                await Context.UpdateReplyAsync(msgProps =>
                    action.CommandRefreshProperties.RefreshAsync(_intoNew, msgProps).GetAwaiter().GetResult());
            }
        }
    }

    public class ActionComponentRunFactory : ActionRunFactory<SocketMessageComponent, BotComponentAction>
    {
        private readonly string _commandTypeName;
        private readonly object[] _idOptions;

        public ActionComponentRunFactory(IServiceProvider services, RequestContext context,
            SocketMessageComponent interaction) : base(services, context, interaction)
        {
            if (string.IsNullOrWhiteSpace(Interaction.Data.CustomId))
                throw new CommandInvalidException();

            //# = refresh component, ^ = refresh into new component
            var splitId = Interaction.Data.CustomId.Split('.');
            _commandTypeName = splitId[0];
            _idOptions = splitId.Skip(1).Cast<object>().ToArray();
        }

        protected override string InteractionNameForLog => Interaction.Data.CustomId;

        protected override BotComponentAction GetAction()
        {
            return ActionService.GetAll().OfType<BotComponentAction>()
                .FirstOrDefault(a => a.GetType().Name == _commandTypeName);
        }

        protected override async Task PopulateParametersAsync(BotComponentAction action)
        {
            var selectOptions = Interaction.Data.Values?.ToArray();
            await action.FillParametersAsync(selectOptions, _idOptions);
        }

        protected override async Task RunActionAsync(BotComponentAction action)
        {
            await action.RunAsync();
        }
    }

    public class ActionTextRunFactory : ActionRunFactory<CommandInfo, BotCommandAction>
    {
        private readonly object[] _parmValues;
        private ActionTextCommandProperties _textProperties;

        public ActionTextRunFactory(IServiceProvider services, RequestContext context, CommandInfo commandInfo,
            object[] parmValues) : base(services, context, commandInfo)
        {
            _parmValues = parmValues;
        }

        protected override string InteractionNameForLog => Interaction.Name;

        protected override BotCommandAction GetAction()
        {
            var action = ActionService.GetAll().OfType<BotCommandAction>().FirstOrDefault(s =>
                s.TextCommandProperties != null && s.TextCommandProperties.Any(t => t.Name == Interaction.Name));

            _textProperties = action.TextCommandProperties.FirstOrDefault(t => t.Name == Interaction.Name);
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