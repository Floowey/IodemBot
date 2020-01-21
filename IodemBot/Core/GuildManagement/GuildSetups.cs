using Discord;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Core
{
    public class GuildSetups
    {
        private static List<GuildSetup> guilds;
        private static readonly string guildsFile = "Resources/Accounts/guilds.json";

        static GuildSetups()
        {
            try
            {
                if (DataStorage.SaveExists(guildsFile))
                {
                    guilds = DataStorage.LoadListFromFile<GuildSetup>(guildsFile).ToList();
                }
                else
                {
                    guilds = new List<GuildSetup>();
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

        public static GuildSetup GetAccount(IGuild user)
        {
            return GetOrCreateGuild(user.Id);
        }

        private static GuildSetup GetOrCreateGuild(ulong id)
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

        private static GuildSetup CreateGuild(ulong id)
        {
            var newAccount = new GuildSetup()
            {
                GuildID = id
            };
            guilds.Add(newAccount);
            SaveGuilds();
            return newAccount;
        }
    }
}