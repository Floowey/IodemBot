﻿using System;
using System.Collections.Generic;
using System.Linq;
using Discord.WebSocket;
using IodemBot.Modules.ColossoBattles;

namespace IodemBot.Core.UserManagement
{
    public static class UserAccounts
    {
        private static readonly List<UserAccount> accounts;
        private static readonly string accountsFile = "Resources/Accounts/accounts.json";

        static UserAccounts()
        {
            try
            {
                if (DataStorage.SaveExists(accountsFile))
                {
                    accounts = DataStorage.LoadListFromFile<UserAccount>(accountsFile).ToList();
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

        public static IReadOnlyCollection<UserAccount> GetAllAccounts()
        {
            return accounts.AsReadOnly();
        }

        public static void SaveAccounts()
        {
            try
            {
                DataStorage.SaveUserAccounts(accounts, accountsFile);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static UserAccount GetAccount(SocketUser user)
        {
            return GetOrCreateAccount(user.Id, user.Username);
        }

        public static List<UserAccount> GetTop(RankEnum type = RankEnum.Level, EndlessMode mode = EndlessMode.Default)
        {
            var sortedList = accounts.OrderByDescending(d => d.TotalXP).ToList();

            switch (type)
            {
                case (RankEnum.Solo):
                    sortedList = accounts.OrderByDescending(d => d.ServerStats.GetStreak(mode).Solo).ToList();
                    break;

                case (RankEnum.Duo):
                case (RankEnum.Trio):
                case (RankEnum.Quad):
                    sortedList = accounts.Where(p => p.ServerStats.GetStreak(mode).GetEntry(type).Item1 > 0)
                        .GroupBy(p => p.ServerStats.GetStreak(mode).GetEntry(type).Item2)
                        .Select(group => group.First())
                        .OrderByDescending(d => d.ServerStats.GetStreak(mode).GetEntry(type).Item1)
                        .ToList();
                    break;

                default: break;
            }
            return sortedList;
        }

        public static int GetRank(SocketUser user, RankEnum type = RankEnum.Level)
        {
            var account = GetAccount(user);
            var sortedList = GetTop(type);
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