using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
