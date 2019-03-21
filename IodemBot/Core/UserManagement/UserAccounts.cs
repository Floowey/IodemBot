using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace IodemBot.Core.UserManagement
{
    public static class UserAccounts
    {
        private static List<UserAccount> accounts;
        private static string accountsFile = "Resources/accounts.json";

        static UserAccounts()
        {
            if (DataStorage.SaveExists(accountsFile))
            {
                accounts = DataStorage.LoadUserAccounts(accountsFile).ToList();
                //foreach (var acc in accounts)
                //{
                //    acc.BattleStats.damageDealt = acc.damageDealt;
                //    acc.BattleStats.HPhealed= acc.HPhealed;
                //    acc.BattleStats.killsByHand = acc.killsByHand;
                //    acc.BattleStats.revives= acc.revives;
                //    acc.BattleStats.soloBattles = acc.soloBattles;
                //    acc.BattleStats.totalTeamMates = acc.totalTeamMates;

                //    acc.ServerStats.channelSwitches = acc.channelSwitches;
                //    acc.ServerStats.ColossoHighestStreak = acc.ColossoHighestStreak;
                //    acc.ServerStats.ColossoStreak = acc.ColossoStreak;
                //    acc.ServerStats.ColossoWins = acc.ColossoWins;
                //    acc.ServerStats.hasQuotedMatthew = acc.hasQuotedMatthew;
                //    acc.ServerStats.hasWrittenCurse = acc.hasWrittenCurse;
                //    acc.ServerStats.mostRecentChannel = acc.mostRecentChannel;
                //    acc.ServerStats.rpsStreak = acc.rpsStreak;
                //    acc.ServerStats.rpsWins = acc.rpsWins;
                //}
                //SaveAccounts();
            }
            else
            {
                accounts = new List<UserAccount>();
                SaveAccounts();
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
            if (account == null) account = CreateUserAccount(id, name);
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

