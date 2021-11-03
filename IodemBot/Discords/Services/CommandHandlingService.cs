using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IodemBot.Discords.Contexts;
using Microsoft.Extensions.DependencyInjection;

namespace IodemBot.Discords.Services
{
    public class CommandHandlingService
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discord;
        private readonly IServiceProvider _services;

        public CommandHandlingService(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _services = services;

            // Hook CommandExecuted to handle post-command-execution logic.
            _commands.CommandExecuted += CommandExecutedAsync;
            // Hook MessageReceived so we can process each message to see
            // if it qualifies as a command.
            _discord.MessageReceived += MessageReceivedAsync;
        }

        public async Task InitializeAsync()
        {
            // Register custom readers.

            // Register modules that are public and inherit ModuleBase<T>.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        public bool HasPrefix(IUserMessage message, IDiscordClient client, ref int argPos)
        {
            return message.HasStringPrefix(Config.bot.cmdPrefix, ref argPos, StringComparison.CurrentCultureIgnoreCase) || message.HasMentionPrefix(client.CurrentUser, ref argPos);
        }

        public async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            // Ignore system messages, or messages from other bots
            if (rawMessage is not SocketUserMessage message) return;
            if (message.Source != MessageSource.User) return;

            // This value holds the offset where the prefix ends
            var argPos = 0;

            // Perform prefix check.
            if (!(message.Channel is IPrivateChannel && message.Channel is SocketChannel && (message.Channel as SocketChannel).Users.Count == 2) &
                !HasPrefix(message, _discord, ref argPos)) return;

            var context = new MessageCommandContext(_discord, message, message.Content[argPos..]);

            // Perform the execution of the command. In this method,
            // the command service will perform precondition and parsing check
            // then execute the command if one is matched.
            _ = _commands.ExecuteAsync(context, argPos, _services);
            // Note that normally a result will be returned by this format, but here
            // we will handle the result in CommandExecutedAsync,

            await Task.Delay(1);
        }

        private readonly List<CommandError> _validErrors = new() { CommandError.BadArgCount, CommandError.MultipleMatches, CommandError.ObjectNotFound, CommandError.ParseFailed, CommandError.UnknownCommand, CommandError.UnmetPrecondition };

        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            if (result?.Error != null && result.Error.Value == CommandError.Exception && result is ExecuteResult executeResult)
            {
                if (executeResult.Exception is CommandInvalidException)
                    await _commands.ExecuteAsync(context, "CommandDidNotWork", _services);
                else
                {
                    Console.WriteLine(executeResult.Exception);
                    await context.Channel.SendMessageAsync("Something went wrong!").ConfigureAwait(false);
                    return;
                }
            }
            else if (!command.IsSpecified || (result?.Error != null && _validErrors.Contains(result.Error.Value)))
            {
                // command is unspecified when there was a search failure (command not found)
                await _commands.ExecuteAsync(context, "CommandDidNotWork", _services);
            }

            // the command was successful, we don't care about this result, unless we want to log that a command succeeded.
            if (result.IsSuccess)
                return;
        }
    }
}