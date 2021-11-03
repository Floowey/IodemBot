using System;
using System.Collections.Generic;
using System.Linq;
using Discord;

namespace IodemBot.Core
{
    public class GuildSettings
    {
        private const string GuildsFile = "Resources/Accounts/guilds.json";
        private static readonly List<GuildSetting> Guilds;

        static GuildSettings()
        {
            try
            {
                if (DataStorage.SaveExists(GuildsFile))
                {
                    Guilds = DataStorage.LoadListFromFile<GuildSetting>(GuildsFile).ToList();
                }
                else
                {
                    Guilds = new List<GuildSetting>();
                    SaveGuilds();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void SaveGuilds()
        {
            try
            {
                DataStorage.SaveUserAccounts(Guilds, GuildsFile);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static GuildSetting GetGuildSettings(IGuild user)
        {
            return GetOrCreateGuild(user.Id);
        }

        private static GuildSetting GetOrCreateGuild(ulong id)
        {
            var result = from a in Guilds
                         where a.GuildId == id
                         select a;

            var account = result.FirstOrDefault() ?? CreateGuild(id);

            return account;
        }

        private static GuildSetting CreateGuild(ulong id)
        {
            var newAccount = new GuildSetting
            {
                GuildId = id
            };
            Guilds.Add(newAccount);
            SaveGuilds();
            return newAccount;
        }
    }
}