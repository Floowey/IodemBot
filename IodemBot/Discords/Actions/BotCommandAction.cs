using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using IodemBot.Discords.Actions.Attributes;

namespace IodemBot.Discords.Actions
{
    public abstract class BotCommandAction : BotAction
    {
        public abstract IActionSlashCommandProperties SlashCommandProperties { get; }
        public abstract List<ActionTextCommandProperties> TextCommandProperties { get; }
        public abstract IActionMessageCommandProperties MessageCommandProperties { get; }
        public abstract IActionUserCommandProperties UserCommandProperties { get; }
        public virtual ActionCommandRefreshProperties CommandRefreshProperties { get; }

        public (bool Selected, IEnumerable<SocketSlashCommandDataOption> Options) SubOptionsForOptions(IEnumerable<SocketSlashCommandDataOption> options, string propertyName)
        {
            ActionParameterSlashAttribute parameter = GetType().GetProperty(propertyName)?.GetCustomAttributes(false).OfType<ActionParameterSlashAttribute>().FirstOrDefault();

            if (parameter == null)
                throw new MissingFieldException($"The {propertyName} property was not found or does not contain the {nameof(ActionParameterSlashAttribute)}.");

            if (parameter.Type != Discord.ApplicationCommandOptionType.SubCommand)
                return (false, null);

            var option = options.FirstOrDefault(t => t.Name == parameter.Name);
            if (option == null)
                return (false, null);

            return (true, option?.Options);
        }

        public TCast ValueForSlashOption<TCast>(IEnumerable<SocketSlashCommandDataOption> options, string propertyName)
        {
            try
            {
                Type type = typeof(TCast);
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>).GetGenericTypeDefinition())
                    type = Nullable.GetUnderlyingType(type);

                ActionParameterSlashAttribute parameter = GetType().GetProperty(propertyName)?.GetCustomAttributes(false).OfType<ActionParameterSlashAttribute>().FirstOrDefault();

                if (parameter == null)
                    throw new MissingFieldException($"The {propertyName} property was not found or does not contain the {nameof(ActionParameterSlashAttribute)}.");

                var option = options.FirstOrDefault(t => t.Name == parameter.Name);

                if (parameter.Type == ApplicationCommandOptionType.SubCommand && type == typeof(bool))
                    return (TCast)Convert.ChangeType(option != null, type);

                object val = option?.Value;
                if (val == null)
                    return default;

                //TCast ret = (TCast)Convert.ChangeType(val, type);
                TCast ret = (TCast)val;
                if (typeof(TCast) == typeof(string))
                    ret = (TCast)Convert.ChangeType(ret.ToString()?.Trim(), type);

                return ret;
            }
            catch
            {
                return default;
            }
        }
    }
}
