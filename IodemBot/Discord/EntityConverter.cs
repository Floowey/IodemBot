using System;
using Discord.WebSocket;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;

namespace IodemBot.Discord
{
    public static class EntityConverter
    {
        static EntityConverter()
        {
            UserConverter = new IodemUserConverter();
        }
        public static IodemUserConverter UserConverter { get; }
        public static UserAccount ConvertUser(SocketUser user)
            => UserConverter.DiscordMemberToUser(user);
    }

    public class IodemUserConverter
    {
        public IodemUserConverter()
        {

        }
        public UserAccount DiscordMemberToUser(SocketUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            var iUser = UserAccountProvider.GetById(user.Id);
            iUser.Name = user is SocketGuildUser u ? u.DisplayName() : user.Username;
            iUser.ImgUrl = user.GetAvatarUrl();
            UserAccountProvider.StoreUser(iUser);
            return iUser;
        }

    }
}
