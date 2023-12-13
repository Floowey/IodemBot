using System;
using System.Linq;
using Discord;
using Discord.WebSocket;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;

namespace IodemBot
{
    public static class EntityConverter
    {
        static EntityConverter()
        {
            UserConverter = new IodemUserConverter();
        }

        public static IodemUserConverter UserConverter { get; }

        public static UserAccount ConvertUser(IUser user)
        {
            return UserConverter.DiscordMemberToUser(user);
        }
    }

    public class IodemUserConverter
    {
        public UserAccount DiscordMemberToUser(IUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            var iUser = UserAccountProvider.GetById(user.Id);
            if(user is SocketUser su){
                iUser.Name = su.DisplayName();
            }
            if (user is SocketGuildUser u)
            {
                iUser.isSupporter |= u.PremiumSince != null;
                iUser.isSupporter |= iUser.TrophyCase.Trophies.Any(t => t.Icon.Contains("Supporter"));
            }

            iUser.ImgUrl = user.GetAvatarUrl();

            //UserAccountProvider.StoreUser(iUser);
            return iUser;
        }
    }
}