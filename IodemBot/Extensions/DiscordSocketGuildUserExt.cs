namespace IodemBot.Extensions
{
    public static class DiscordSocketGuildUserExt
    {
        public static string DisplayName(this Discord.WebSocket.SocketGuildUser user)
        {
            return user.Nickname ?? user.Username;
        }
    }
}