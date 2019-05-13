using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Core.UserManagement
{
    public static class UserAccounts
    {
        private static List<UserAccount> accounts;
        private static readonly string accountsFile = "Resources/accounts.json";

        static UserAccounts()
        {
            try
            {
                if (DataStorage.SaveExists(accountsFile))
                {
                    accounts = DataStorage.LoadUserAccounts(accountsFile).ToList();
                }
                else
                {
                    accounts = new List<UserAccount>();
                    SaveAccounts();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void SaveAccounts()
        {
            try
            {
                DataStorage.SaveUserAccounts(accounts, accountsFile);
            }
            catch { }
        }

        public static UserAccount GetAccount(SocketUser user)
        {
            return GetOrCreateAccount(user.Id, user.Username);
        }

        public static UserAccount[] GetTop(int number)
        {
            var sortedList = accounts.OrderByDescending(d => d.XP).Take(number);
            return sortedList.ToArray();
        }

        public static int GetRank(SocketUser user)
        {
            var account = GetAccount(user);
            var sortedList = accounts.OrderByDescending(d => d.XP).ToList();
            return sortedList.IndexOf(account);
        }

        private static UserAccount GetOrCreateAccount(ulong id, string name)
        {
            var result = from a in accounts
                         where a.ID == id
                         select a;

            var account = result.FirstOrDefault();
            if (account == null)
            {
                account = CreateUserAccount(id, name);
            }

            return account;
        }

        private static UserAccount CreateUserAccount(ulong id, string name)
        {
            var newAccount = new UserAccount()
            {
                ID = id,
                Name = name
            };
            accounts.Add(newAccount);
            SaveAccounts();
            return newAccount;
        }
    }
}