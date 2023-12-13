using Discord.WebSocket;

namespace IodemBot.Extensions
{
    public static class DiscordSocketUserExt
    {
        public static string DisplayName(this SocketUser user)
        {
            string name = user.GlobalName ?? user.Username;
            if(user is SocketGuildUser u)
                name = u.Nickname ?? name;
            return name;
        }
    }
}