using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace IodemBot.Core.UserManagement
{
    public static class UserAccountProvider
    {
        private static readonly IPersistentStorage<UserAccount> PersistentStorage;

        private static readonly Dictionary<Tuple<RankEnum, EndlessMode>, LeaderBoard> LeaderBoards
            = new();

        public static Calendar cal = new CultureInfo("en-US").Calendar;

        public static string weekKey
        {
            get
            {
                int week = cal.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                return $"{DateTime.Now.Year}-{week}";
            }
        }

        public static string monthKey => $"{DateTime.Now.Year}-{DateTime.Now.Month}";
        public static DateTime CurrentMonth => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        static UserAccountProvider()
        {
            //_persistentStorage = new PersistentStorage<UserAccount>();
            PersistentStorage = new UserDataFileStorage();
            foreach (var rank in new[] { RankEnum.Solo, RankEnum.Duo, RankEnum.Trio, RankEnum.Quad })
                foreach (var mode in new[] { EndlessMode.Default, EndlessMode.Legacy })
                    LeaderBoards.Add(new Tuple<RankEnum, EndlessMode>(rank, mode),
                        new LeaderBoard(u =>
                            (ulong)(u.ServerStats.GetStreak(mode).GetEntry(rank).Item1 +
                                     u.ServerStatsTotal.GetStreak(mode).GetEntry(rank).Item1)));

            LeaderBoards.Add(new Tuple<RankEnum, EndlessMode>(RankEnum.AllTime, EndlessMode.Default),
                new LeaderBoard(u => u.TotalXp));

            LeaderBoards.Add(new Tuple<RankEnum, EndlessMode>(RankEnum.Week, EndlessMode.Default),
                new LeaderBoard(u => (ulong)u.DailyXP
                .Where(kv => kv.Key.Year == DateTime.Now.Year &&
                cal.GetWeekOfYear(kv.Key, CalendarWeekRule.FirstDay, DayOfWeek.Monday) == cal.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                .Select(kv => (decimal)kv.Value).Sum())
            );

            LeaderBoards.Add(new Tuple<RankEnum, EndlessMode>(RankEnum.Month, EndlessMode.Default),
                new LeaderBoard(u => (ulong)(u.DailyXP.Where(kv => kv.Key >= CurrentMonth).Select(kv => (decimal)kv.Value).Sum()))
            );

            foreach (var user in GetAllUsers())
            {
                if (user == null)
                {
                    Console.WriteLine("User was null");
                    continue;
                }

                foreach (var lb in LeaderBoards.Values) lb.Set(user);
            }
        }

        public static LeaderBoard GetLeaderBoard(RankEnum type = RankEnum.AllTime, EndlessMode mode = EndlessMode.Default)
        {
            if (type == RankEnum.AllTime || type == RankEnum.Week || type == RankEnum.Month) mode = EndlessMode.Default;
            return LeaderBoards[new Tuple<RankEnum, EndlessMode>(type, mode)];
        }

        public static UserAccount GetById(ulong userId)
        {
            var user = PersistentStorage.RestoreSingle(userId);
            return EnsureExists(user, userId);
        }

        public static void StoreUser(UserAccount user)
        {
            if (PersistentStorage.Exists(user.Id))
                PersistentStorage.Update(user);
            else
                PersistentStorage.Store(user);

            foreach (var lb in LeaderBoards.Values) lb.Set(user);
        }

        public static void RemoveUser(UserAccount user)
        {
            if (PersistentStorage.Exists(user.Id)) PersistentStorage.Remove(user.Id);
        }

        public static IEnumerable<UserAccount> GetAllUsers()
        {
            return PersistentStorage.RestoreAll();
        }

        private static UserAccount EnsureExists(UserAccount user, ulong userId)
        {
            if (user is null)
            {
                user = UserAccounts.GetAccount(userId);
                StoreUser(user);
            }

            return user;
        }

        public class LeaderBoard : IDictionary<ulong, ulong>
        {
            private readonly List<KeyValuePair<ulong, ulong>> _dict = new();
            private readonly Func<UserAccount, ulong> _func;

            public LeaderBoard(Func<UserAccount, ulong> function)
            {
                _func = function;
            }

            public ulong this[ulong key]
            {
                get => _dict.FirstOrDefault(e => e.Key == key).Value;
                set
                {
                    Remove(key);
                    _dict.Add(new KeyValuePair<ulong, ulong>(key, value));
                }
            }

            public ICollection<ulong> Keys
            {
                get
                {
                    Sort();
                    return _dict.Select(e => e.Key).ToList();
                }
            }

            public ICollection<ulong> Values
            {
                get
                {
                    Sort();
                    return _dict.Select(e => e.Value).ToList();
                }
            }

            public int Count => _dict.Count;

            public bool IsReadOnly => false;

            public void Add(ulong key, ulong value)
            {
                _dict.Add(new KeyValuePair<ulong, ulong>(key, value));
            }

            public void Add(KeyValuePair<ulong, ulong> item)
            {
                _dict.Add(item);
            }

            public void Clear()
            {
                _dict.Clear();
            }

            public bool Contains(KeyValuePair<ulong, ulong> item)
            {
                return _dict.Contains(item);
            }

            public bool ContainsKey(ulong key)
            {
                return _dict.Any(e => e.Key == key);
            }

            public void CopyTo(KeyValuePair<ulong, ulong>[] array, int arrayIndex)
            {
                _dict.CopyTo(array, arrayIndex);
            }

            public IEnumerator<KeyValuePair<ulong, ulong>> GetEnumerator()
            {
                Sort();
                return _dict.GetEnumerator();
            }

            public bool Remove(ulong key)
            {
                return _dict.RemoveAll(e => e.Key == key) > 0;
            }

            public bool Remove(KeyValuePair<ulong, ulong> item)
            {
                return _dict.Remove(item);
            }

            public bool TryGetValue(ulong key, out ulong value)
            {
                try
                {
                    if (ContainsKey(key))
                    {
                        value = this[key];
                        return true;
                    }

                    value = 0;
                    return false;
                }
                catch (InvalidOperationException ioe)
                {
                    Console.WriteLine(ioe);
                    value = 0;
                    return false;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_dict).GetEnumerator();
            }

            private void Sort()
            {
                _dict.Sort((x, y) => y.Value.CompareTo(x.Value));
            }

            public int IndexOf(ulong key)
            {
                return IndexOfKey(key);
            }

            public int IndexOfKey(ulong key)
            {
                Sort();
                return _dict.FindIndex(k => k.Key == key);
            }

            public void Add(UserAccount user)
            {
                var val = _func.Invoke(user);
                if (val > 0) Add(user.Id, _func.Invoke(user));
            }

            public void Set(UserAccount user)
            {
                try
                {
                    var val = _func.Invoke(user);
                    if (val > 0)
                    {
                        if (ContainsKey(user.Id))
                            this[user.Id] = val;
                        else
                            Add(user.Id, val);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}