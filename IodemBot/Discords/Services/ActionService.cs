﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using IodemBot.Discords.Actions;
using IodemBot.Discords.Actions.Attributes;
using IodemBot.Discords.Contexts;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace IodemBot.Discords.Services
{
    public class ActionService
    {
        private readonly DiscordSocketClient _discord;

        private readonly ConcurrentDictionary<ulong, CollectorLogic> _inProgressCollectors = new();
        private readonly IServiceProvider _services;

        public ActionService(IServiceProvider services)
        {
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _services = services;
        }

        public Task InitializeAsync()
        {
            _discord.Ready += ClientReadyAsync;
            _discord.InteractionCreated += Client_InteractionCreated;
            //_discord.JoinedGuild += ClientJoinedGuildAsync;

            return Task.CompletedTask;
        }

        public Task StartSlash()
        {
            _discord.InteractionCreated -= Client_InteractionCreated;
            _discord.InteractionCreated += Client_InteractionCreated;
            return Task.CompletedTask;
        }

        public Task StopSlash()
        {
            _discord.InteractionCreated -= Client_InteractionCreated;
            return Task.CompletedTask;
        }

        public List<BotAction> GetAll()
        {
            var allActions = new List<BotAction>();
            foreach (var type in typeof(BotAction).Assembly.GetTypes().Where(myType =>
                myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(BotAction))))
                allActions.Add((BotAction)Activator.CreateInstance(type));

            return allActions;
        }

        private async Task ClientReadyAsync()
        {
            _discord.Ready -= ClientReadyAsync;

            try
            {
                // await AddGlobalCommands();
                //PurgeAllGuildCommands();
            }
            catch (HttpException exception)
            {
                // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                Console.WriteLine(json);
            }
        }

        public async Task AddGlobalCommands()
        {
            List<ApplicationCommandProperties> properties = new();
            var slashActions = GetAll().OfType<BotCommandAction>().Where(a =>
                a.SlashCommandProperties is ActionGlobalSlashCommandProperties);

            foreach (var slashAction in slashActions) properties.Add(BuildSlashCommandProperties(slashAction));

            var messageActions = GetAll().OfType<BotCommandAction>().Where(a =>
                a.MessageCommandProperties is ActionGlobalMessageCommandProperties);

            foreach (var messageAction in messageActions) properties.Add(BuildMessageCommandProperties(messageAction));

            if (properties.Count > 0)
            {
                if (!_discord.Rest.GetGlobalApplicationCommands().GetAwaiter().GetResult().Any())
                {
                    await _discord.Rest.CreateGlobalCommand(properties.First());
                    Console.WriteLine($"Only added {properties.First().Name}");
                }
                else
                {
                    await _discord.Rest.BulkOverwriteGlobalCommands(properties.ToArray());
                }
            }

            /* See below
            var guildIds = (await _discord.Rest.GetGuildsAsync()).Select(g => g.Id);
            foreach (var guildId in guildIds)
            {
                var guild = _discord.GetGuild(guildId);
                if (guild != null)
                    await SetOwnerPermissionsAsync(guild);
            }
            */
        }

        public async Task AddGuildCommand(ulong guildId, string name)
        {
            var slashActions = GetAll().OfType<BotCommandAction>().Where(a =>
                a.SlashCommandProperties is ActionGuildSlashCommandProperties && a.MessageCommandProperties.Name == name);

            foreach (var slashAction in slashActions)
            {
                var commandProperties = BuildSlashCommandProperties(slashAction);
                await _discord.Rest.CreateGuildCommand(commandProperties, guildId);
            }

            var messageActions = GetAll().OfType<BotCommandAction>().Where(a =>
                a.MessageCommandProperties is ActionGuildMessageCommandProperties && a.MessageCommandProperties.Name == name);

            foreach (var messageAction in messageActions)
            {
                var commandProperties = BuildMessageCommandProperties(messageAction);
                await _discord.Rest.CreateGuildCommand(commandProperties, guildId);
            }

            var userActions = GetAll().OfType<BotCommandAction>().Where(a =>
                a.UserCommandProperties is ActionGuildUserCommandProperties && a.UserCommandProperties.Name == name);

            foreach (var userAction in userActions)
            {
                var commandProperties = BuildUserCommandProperties(userAction);
                await _discord.Rest.CreateGuildCommand(commandProperties, guildId);
            }
        }

        public async Task AddAllGuildCommands(ulong guildId)
        {
            List<ApplicationCommandProperties> properties = new();
            var slashActions = GetAll().OfType<BotCommandAction>().Where(a =>
                a.SlashCommandProperties is ActionGuildSlashCommandProperties);

            foreach (var slashAction in slashActions) properties.Add(BuildSlashCommandProperties(slashAction));

            var messageActions = GetAll().OfType<BotCommandAction>().Where(a =>
                a.MessageCommandProperties is ActionGuildMessageCommandProperties);

            foreach (var messageAction in messageActions) properties.Add(BuildMessageCommandProperties(messageAction));

            var userActions = GetAll().OfType<BotCommandAction>().Where(a =>
                a.UserCommandProperties is ActionGuildUserCommandProperties);

            foreach (var userAction in userActions) properties.Add(BuildUserCommandProperties(userAction));
            if (properties.Count > 0)
                await _discord.Rest.BulkOverwriteGuildCommands(properties.ToArray(), guildId);
        }

        private SlashCommandProperties BuildSlashCommandProperties(BotCommandAction action)
        {
            var newCommand = new SlashCommandBuilder();
            newCommand.WithName(action.SlashCommandProperties.Name);
            newCommand.WithDescription(action.SlashCommandProperties.Description);

            var parameters = action.GetParameters<ActionParameterSlashAttribute>()?.OrderBy(p => p.Attribute.Order);
            if (parameters != null)
            {
                var filteredParameters = parameters
                    .Where(p => p.Attribute.ParentNames == null || !p.Attribute.ParentNames.Any()).ToList();
                if (filteredParameters.Any())
                    foreach (var p in filteredParameters)
                    {
                        var option = BuildOptionFromParameter(p.Property, p.Attribute, parameters.ToList());

                        newCommand.AddOption(option);
                    }
            }
            //newCommand.WithDefaultPermission(action.RequiredPermissions == null);

            return newCommand.Build();
        }

        private MessageCommandProperties BuildMessageCommandProperties(BotCommandAction action)
        {
            var newCommand = new MessageCommandBuilder();
            newCommand.WithName(action.MessageCommandProperties.Name);

            return newCommand.Build();
        }

        private UserCommandProperties BuildUserCommandProperties(BotCommandAction action)
        {
            var newCommand = new UserCommandBuilder();
            newCommand.WithName(action.UserCommandProperties.Name);

            return newCommand.Build();
        }

        private void PurgeAllGuildCommands()
        {
            _ = Task.Run(async () =>
            {
                foreach (var guildSummary in await _discord.Rest.GetGuildSummariesAsync().FlattenAsync())
                {
                    var commands = await _discord.Rest.GetGuildApplicationCommands(guildSummary.Id);
                    if (commands.Any()) await commands.FirstOrDefault()?.DeleteAsync();
                    await Task.Delay(500);
                }
            });
        }

        public async Task PurgeGuildCommandsAsync(ulong guildId)
        {
            await (await _discord.Rest.GetGuildApplicationCommands(guildId)).FirstOrDefault()?.DeleteAsync();
        }

        public async Task DeleteGuildCommandByNameAsync(ulong guildId, string name)
        {
            await (await _discord.Rest.GetGuildApplicationCommands(guildId)).FirstOrDefault(c => c.Name == name)
                ?.DeleteAsync();
        }

        private SlashCommandOptionBuilder BuildOptionFromParameter(PropertyInfo property,
            ActionParameterSlashAttribute attribute,
            List<(PropertyInfo Property, ActionParameterSlashAttribute Attribute)> parameters)
        {
            var option = new SlashCommandOptionBuilder
            {
                Name = attribute.Name,
                Description = attribute.Description,
                IsRequired = attribute.Required,
                Type = attribute.Type,
                IsAutocomplete = attribute.AutoComplete,
                IsDefault = attribute.DefaultSubCommand ? true : null,
            };

            var stringChoices = property.GetCustomAttributes(false).OfType<ActionParameterOptionStringAttribute>()
                ?.OrderBy(c => c.Order);
            if (stringChoices != null && stringChoices.Any())
                foreach (var c in stringChoices)
                    option.AddChoice(c.Name, c.Value);

            var intChoices = property.GetCustomAttributes(false).OfType<ActionParameterOptionIntAttribute>()
                ?.OrderBy(c => c.Order);
            if (intChoices != null && intChoices.Any())
                foreach (var c in intChoices)
                    option.AddChoice(c.Name, c.Value);

            var filteredParameters = parameters.Where(p =>
                p.Attribute.ParentNames != null && p.Attribute.ParentNames.Contains(attribute.Name)).ToList();
            if (filteredParameters != null && filteredParameters.Any())
                foreach (var p in filteredParameters)
                {
                    var subOption = BuildOptionFromParameter(p.Property, p.Attribute, filteredParameters);

                    option.AddOption(subOption);
                }

            return option;
        }

        public void RegisterCollector(CollectorLogic collector)
        {
            _inProgressCollectors.GetOrAdd(collector.MessageId, collector);
        }

        public void UnregisterCollector(CollectorLogic collector)
        {
            _inProgressCollectors.Remove(collector.MessageId, out _);
        }

        public bool CollectorAvailable(ulong messageId)
        {
            return _inProgressCollectors.TryGetValue(messageId, out _);
        }

        public async Task<(bool Success, MessageBuilder MessageBuilder)> FireCollectorAsync(IUser userData,
            ulong messageId, object[] idParams, object[] selectParams)
        {
            _inProgressCollectors.TryGetValue(messageId, out var collector);
            if (collector == null || collector.Execute == null)
                return (false,
                    new MessageBuilder(userData, "I couldn't find that action anymore. Maybe you were too late?", false,
                        null));
            if (collector.OnlyOriginalUserAllowed && collector.OriginalUserId != userData.Id)
                return (false,
                    new MessageBuilder(userData, "Sorry, for this message, only the calling user gets to pick!", false,
                        null));

            var builder = await collector.Execute(userData, messageId, idParams, selectParams);
            return (true, builder);
        }

        /*
         * For now, just working without this. It's not robust enough. Will be better to build my own.
         *
        private async Task ClientJoinedGuildAsync(SocketGuild guild)
        {
            try
            {
                await SetOwnerPermissionsAsync(guild);
            }
            catch (ApplicationCommandException exception)
            {
                // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception.Error, Formatting.Indented);
                Console.WriteLine(json);
            }
        }

        private async Task SetOwnerPermissionsAsync(SocketGuild guild)
        {
            var globalCommands = (await _discord.Rest.GetGlobalApplicationCommands()).Where(c => !c.DefaultPermission);
            List<ulong> commandIds = globalCommands.Where(c => !c.DefaultPermission).Select(c => c.Id).ToList();

        //use context for this now? there's a method
            var application = await _discord.GetApplicationInfoAsync().ConfigureAwait(false);

            ulong botOwnerId = application.Owner.Id;
            ulong guildOwnerId = guild.OwnerId;

            List<ulong> modRoleIds = guild.Roles.Where(r => r.Permissions.ManageRoles).Take(8).Select(r => r.Id)?.ToList();

            var permDict = new Dictionary<ulong, ApplicationCommandPermission[]>();
            foreach (ulong id in commandIds)
            {
                var perms = new List<ApplicationCommandPermission>
                {
                    new ApplicationCommandPermission(botOwnerId, ApplicationCommandPermissionTarget.User, true),
                    new ApplicationCommandPermission(guildOwnerId, ApplicationCommandPermissionTarget.User, true),
                };
                foreach (ulong roleId in modRoleIds)
                    perms.Add(new ApplicationCommandPermission(roleId, ApplicationCommandPermissionTarget.Role, true));

                permDict.Add(id, perms.ToArray());
            }

            if (permDict.Any())
            {
                await _discord.Rest.BatchEditGuildCommandPermissions(guild.Id, permDict);
            }
        }
        */

        private async Task Client_InteractionCreated(SocketInteraction arg)
        {
            var context = new RequestInteractionContext(arg, _discord);
            var actionRunFactory = ActionRunFactory.Find(_services, context, arg);
            if (actionRunFactory != null)
                await actionRunFactory.RunActionAsync();
        }
    }
}