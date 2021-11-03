using System;
using System.Collections.Generic;
using System.Linq;
using Discord.WebSocket;

namespace IodemBot.Core.UserManagement
{
    public static class UserAccounts
    {
        private static readonly List<UserAccount> Accounts;

        //private static readonly string accountsFile = "Resources/Accounts/accounts.json";
        private static readonly string AccountsFile = "Resources/Accounts/accounts.json";

        static UserAccounts()
        {
            try
            {
                if (DataStorage.SaveExists(AccountsFile))
                {
                    Accounts = DataStorage.LoadListFromFile<UserAccount>(AccountsFile).ToList();
                }
                else
                {
                    Accounts = new List<UserAccount>();
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
            return Accounts.AsReadOnly();
        }

        public static void SaveAccounts()
        {
            try
            {
                //DataStorage.SaveUserAccounts(accounts, accountsFile);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static UserAccount GetAccount(SocketUser user)
        {
            return GetOrCreateAccount(user.Id);
        }

        public static UserAccount GetAccount(ulong id)
        {
            return GetOrCreateAccount(id);
        }

        public static List<UserAccount> GetTop(RankEnum type = RankEnum.Level, EndlessMode mode = EndlessMode.Default)
        {
            List<UserAccount> sortedList = null;
            switch (type)
            {
                case RankEnum.Level:
                case RankEnum.Solo:
                    try
                    {
                        sortedList = UserAccountProvider.GetLeaderBoard(type, mode)
                            .Take(10)
                            .ToList()
                            .Select(id => UserAccountProvider.GetById(id.Key))
                            .ToList();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    break;

                case RankEnum.Duo:
                case RankEnum.Trio:
                case RankEnum.Quad:
                    sortedList = UserAccountProvider.GetLeaderBoard(type, mode)
                        .Take(20)
                        .Select(id => UserAccountProvider.GetById(id.Key))
                        .GroupBy(p =>
                            (p.ServerStats.GetStreak(mode) + p.ServerStatsTotal.GetStreak(mode)).GetEntry(type).Item2)
                        .Select(group => group.First())
                        .OrderByDescending(d =>
                            (d.ServerStats.GetStreak(mode) + d.ServerStatsTotal.GetStreak(mode)).GetEntry(type).Item1)
                        .ToList();
                    break;
            }

            return sortedList;
        }

        public static int GetRank(UserAccount user, RankEnum type = RankEnum.Level,
            EndlessMode mode = EndlessMode.Default)
        {
            return UserAccountProvider.GetLeaderBoard(type, mode).IndexOf(user.Id);
        }

        private static UserAccount GetOrCreateAccount(ulong id)
        {
            var result = from a in Accounts
                         where a.Id == id
                         select a;

            var account = result.FirstOrDefault() ?? CreateUserAccount(id);

            return account;
        }

        private static UserAccount CreateUserAccount(ulong id)
        {
            var newAccount = new UserAccount
            {
                Id = id
            };
            Accounts.Add(newAccount);
            SaveAccounts();
            return newAccount;
        }
    }
}