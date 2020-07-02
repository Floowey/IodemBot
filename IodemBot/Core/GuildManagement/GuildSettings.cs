using Discord;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Core
{
    public class GuildSettings
    {
        private static List<GuildSetting> guilds;
        private static readonly string guildsFile = "Resources/Accounts/guilds.json";

        static GuildSettings()
        {
            try
            {
                if (DataStorage.SaveExists(guildsFile))
                {
                    guilds = DataStorage.LoadListFromFile<GuildSetting>(guildsFile).ToList();
                }
                else
                {
                    guilds = new List<GuildSetting>();
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
                DataStorage.SaveUserAccounts(guilds, guildsFile);
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
            var result = from a in guilds
                         where a.GuildID == id
                         select a;

            var account = result.FirstOrDefault();
            if (account == null)
            {
                account = CreateGuild(id);
            }

            return account;
        }

        private static GuildSetting CreateGuild(ulong id)
        {
            var newAccount = new GuildSetting()
            {
                GuildID = id
            };
            guilds.Add(newAccount);
            SaveGuilds();
            return newAccount;
        }
    }
}