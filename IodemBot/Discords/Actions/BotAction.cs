using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using IodemBot.Discords.Actions.Attributes;
using IodemBot.Discords.Contexts;

namespace IodemBot.Discords.Actions
{
    public abstract class BotAction
    {
        public IServiceProvider ServiceProvider { get; set; }
        public RequestContext Context { get; set; }

        public abstract EphemeralRule EphemeralRule { get; }
        public abstract bool GuildsOnly { get; }
        public abstract GuildPermissions? RequiredPermissions { get; }

        protected Task<(bool, string)> SuccessFullResult => Task.FromResult((true, (string)null));

        public void Initialize(IServiceProvider services, RequestContext context)
        {
            ServiceProvider = services;
            Context = context;
        }

        public bool ValidateParameters<T>(string filterCommandName = null) where T : IActionParameterAttribute
        {
            //Required check (all we have right now)
            var parameterProperties = GetType().GetProperties()
                .SelectMany(
                    p => p.GetCustomAttributes(false).OfType<T>().Select(a => new { Property = p, Attribute = a }))
                .Where(p => p.Attribute.Required && (p.Attribute is not ActionParameterSlashAttribute apsa ||
                                                     apsa.ParentNames == null ||
                                                     !apsa.ParentNames
                                                         .Any())); //for now, don't worry about validating child properties.

            if (!parameterProperties.Any())
                return true;
            if (!string.IsNullOrWhiteSpace(filterCommandName))
                parameterProperties = parameterProperties.Where(p =>
                    p.Attribute.FilterCommandNames == null ||
                    p.Attribute.FilterCommandNames.Contains(filterCommandName));

            foreach (var p in parameterProperties)
            {
                var value = p.Property.GetValue(this);
                if (value == null)
                    return false;
            }

            return true;
        }

        public IEnumerable<(PropertyInfo Property, T Attribute)> GetParameters<T>(string filterCommandName = null)
            where T : IActionParameterAttribute
        {
            var parameterProperties = GetType().GetProperties().SelectMany(p =>
                p.GetCustomAttributes(false).OfType<T>().Select(a => (Property: p, Attribute: a)));

            if (!parameterProperties.Any())
                return null;
            if (!string.IsNullOrWhiteSpace(filterCommandName))
                parameterProperties = parameterProperties.Where(p =>
                    p.Attribute.FilterCommandNames == null ||
                    p.Attribute.FilterCommandNames.Contains(filterCommandName));

            return parameterProperties;
        }

        public async Task<(bool Success, string Message)> CheckPreconditionsAsync()
        {
            if (RequiredPermissions.HasValue)
            {
                if (!RequiredPermissions.Value.ToList().Any())
                {
                    var botOwnerId = await Context.GetBotOwnerIdAsync();
                    if (botOwnerId != Context.User.Id)
                        return (false, "You need to be the bot creator to run that command! Sorry!");
                }
                else
                {
                    if (!HasCorrectPermissions(Context.User as IGuildUser))
                        return (false, "You don't have the permissions to run that command! Sorry!");
                }
            }

            return await CheckCustomPreconditionsAsync();
        }

        private bool HasCorrectPermissions(IGuildUser user)
        {
            var guildPermissions = RequiredPermissions.Value;
            var userPermissions = user.GuildPermissions;

            return userPermissions.Administrator ||
                   guildPermissions.ToList().All(p => userPermissions.Has(p));
        }

        protected abstract Task<(bool Success, string Message)> CheckCustomPreconditionsAsync();

        public abstract Task RunAsync();

        public (bool Success, string Message) IsGameCommandAllowedInGuild()
        {
            if (GuildsOnly && Context.Channel is IDMChannel)
                return (false, "You can't do that here! Find a server that I'm in, instead!");

            return (true, null);
        }
    }
}