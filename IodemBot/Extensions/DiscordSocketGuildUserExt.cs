using Discord.WebSocket;

namespace IodemBot.Extensions
{
    public static class DiscordSocketGuildUserExt
    {
        public static string DisplayName(this SocketGuildUser user)
        {
            return user.Nickname ?? user.Username;
        }
    }
}