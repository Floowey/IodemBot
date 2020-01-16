using Discord;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace IodemBot.Extensions
{
    public static class DiscordExtensions
    {
        // Extensions for Discord Class SocketGuild
        public static SocketGuildUser FirstUserByName(this SocketGuild guild, string name)
        {
            if (MentionUtils.TryParseUser(name, out var id))
            {
                return guild.Users.FirstOrDefault(user => user.Id == id);
            }

            return guild.Users.FirstOrDefault(user =>
                Regex.IsMatch(user.Username, name, RegexOptions.IgnoreCase) ||
                Regex.IsMatch(user.Nickname ?? "", name, RegexOptions.IgnoreCase)
            );
        }

        public static SocketTextChannel FirstTextChannelByName(this SocketGuild guild, string name)
        {
            if (MentionUtils.TryParseChannel(name, out var id))
            {
                return guild.TextChannels.FirstOrDefault(channel => channel.Id == id);
            }

            return guild.TextChannels.FirstOrDefault(channel =>
                Regex.IsMatch(channel.Name, name, RegexOptions.IgnoreCase)
            );
        }

        public static SocketVoiceChannel FirstVoiceChannelByName(this SocketGuild guild, string name)
        {
            if (MentionUtils.TryParseChannel(name, out var id))
            {
                return guild.VoiceChannels.FirstOrDefault(channel => channel.Id == id);
            }

            return guild.VoiceChannels.FirstOrDefault(channel =>
                Regex.IsMatch(channel.Name, name, RegexOptions.IgnoreCase)
            );
        }

        public static SocketRole FirstRoleByName(this SocketGuild guild, string name)
        {
            if (MentionUtils.TryParseRole(name, out var id))
            {
                return guild.Roles.FirstOrDefault(role => role.Id == id);
            }

            return guild.Roles.FirstOrDefault(role =>
                Regex.IsMatch(role.Name, name, RegexOptions.IgnoreCase)
            );
        }

        /// <summary>
        /// Extended function that creates an awaitable Task which resolves in the first SocketMessage send in this channel
        /// that matches the provided filter - times out after a set time
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="filter">Optional - if not provided first message in channel is match</param>
        /// <param name="timeoutInMs">Optional - if not provided 30 seconds</param>
        /// <returns>Awaitable Task which resolves in the first SocketMessage that matches the filter</returns>
        public static async Task<SocketMessage> AwaitMessage(this IMessageChannel channel, Func<SocketMessage, bool> filter = null, int timeoutInMs = 30000)
        {
            SocketMessage responseMessage = null;
            var cancler = new CancellationTokenSource();
            var waiter = Task.Delay(timeoutInMs, cancler.Token);

            // Adding function that handles filtering and
            // assigning the respondMessage the correct value
            Global.Client.MessageReceived += OnMessageReceived;
            // Waiting for the timeout to run out or the task.Delay to be canceled due to a matched message
            try { await waiter; }
            catch (TaskCanceledException) { }
            finally
            {
                // Remove the function from the event handler list
                Global.Client.MessageReceived -= OnMessageReceived;
            }
            return responseMessage;

            Task OnMessageReceived(SocketMessage message)
            {
                // look for a response on the same channel, and ignore messages from any bots (which includes itself)
                if (message.Channel.Id != channel.Id || message.Author.IsBot)
                {
                    return Task.CompletedTask;
                }

                if (filter != null && !filter(message))
                {
                    return Task.CompletedTask;
                }

                responseMessage = message;
                cancler.Cancel();
                return Task.CompletedTask;
            }
        }
    }
}