using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace IodemBot.Core.UserManagement
{
    public static class UserAccountProvider
    {
        private static readonly IPersistentStorage<UserAccount> _persistentStorage;

        private static readonly Dictionary<Tuple<RankEnum, EndlessMode>, LeaderBoard> leaderBoards
            = new Dictionary<Tuple<RankEnum, EndlessMode>, LeaderBoard>();

        static UserAccountProvider()
        {
            //_persistentStorage = new PersistentStorage<UserAccount>();
            _persistentStorage = new UserDataFileStorage();
            foreach (var rank in new[] { RankEnum.Solo, RankEnum.Duo, RankEnum.Trio, RankEnum.Quad })
            {
                foreach (var mode in new[] { EndlessMode.Default, EndlessMode.Legacy })
                {
                    leaderBoards.Add(new Tuple<RankEnum, EndlessMode>(rank, mode), new LeaderBoard(u => (ulong)(u.ServerStats.GetStreak(mode).GetEntry(rank).Item1 + u.ServerStatsTotal.GetStreak(mode).GetEntry(rank).Item1)));
                }
            }
            leaderBoards.Add(new Tuple<RankEnum, EndlessMode>(RankEnum.Level, EndlessMode.Default), new LeaderBoard(u => u.TotalXP));

            foreach (var user in GetAllUsers())
            {
                if (user == null)
                {
                    Console.WriteLine("User was null");
                    continue;
                }
                foreach (var lb in leaderBoards.Values)
                {
                    lb.Set(user);
                }
            }
        }

        public static LeaderBoard GetLeaderBoard(RankEnum type = RankEnum.Level, EndlessMode mode = EndlessMode.Default)
        {
            if (type == RankEnum.Level) mode = EndlessMode.Default;
            return leaderBoards[new Tuple<RankEnum, EndlessMode>(type, mode)];
        }

        public static UserAccount GetById(ulong userId)
        {
            var user = _persistentStorage.RestoreSingle(userId);
            return EnsureExists(user, userId);
        }

        public static void StoreUser(UserAccount user)
        {
            if (_persistentStorage.Exists(user.ID))
            {
                _persistentStorage.Update(user);
            }
            else
            {
                _persistentStorage.Store(user);
            }

            foreach (var lb in leaderBoards.Values)
            {
                lb.Set(user);
            }
        }

        public static void RemoveUser(UserAccount user)
        {
            if (_persistentStorage.Exists(user.ID))
            {
                _persistentStorage.Remove(user.ID);
            }
        }

        public static IEnumerable<UserAccount> GetAllUsers()
            => _persistentStorage.RestoreAll();

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
            private readonly Func<UserAccount, ulong> func;
            private readonly List<KeyValuePair<ulong, ulong>> dict = new List<KeyValuePair<ulong, ulong>>();

            public LeaderBoard(Func<UserAccount, ulong> function)
            {
                func = function;
            }

            private void Sort()
            {
                dict.Sort((x, y) => y.Value.CompareTo(x.Value));
            }

            public ulong this[ulong key]
            {
                get => dict.FirstOrDefault(e => e.Key == key).Value;
                set
                {
                    Remove(key);
                    dict.Add(new KeyValuePair<ulong, ulong>(key, value));
                }
            }

            public ICollection<ulong> Keys
            {
                get
                {
                    Sort();
                    return dict.Select(e => e.Key).ToList();
                }
            }

            public ICollection<ulong> Values
            {
                get
                {
                    Sort();
                    return dict.Select(e => e.Value).ToList();
                }
            }

            public int IndexOf(ulong key)
            {
                return IndexOfKey(key);
            }

            public int IndexOfKey(ulong key)
            {
                Sort();
                return dict.FindIndex(k => k.Key == key);
            }

            public int Count => dict.Count;

            public bool IsReadOnly => false;

            public void Add(UserAccount user)
            {
                var val = func.Invoke(user);
                if (val > 0)
                {
                    Add(user.ID, func.Invoke(user));
                }
            }

            public void Add(ulong key, ulong value)
            {
                dict.Add(new KeyValuePair<ulong, ulong>(key, value));
            }

            public void Add(KeyValuePair<ulong, ulong> item)
            {
                dict.Add(item);
            }

            public void Clear()
            {
                dict.Clear();
            }

            public bool Contains(KeyValuePair<ulong, ulong> item)
            {
                return dict.Contains(item);
            }

            public bool ContainsKey(ulong key)
            {
                return dict.Any(e => e.Key == key);
            }

            public void CopyTo(KeyValuePair<ulong, ulong>[] array, int arrayIndex)
            {
                dict.CopyTo(array, arrayIndex);
            }

            public IEnumerator<KeyValuePair<ulong, ulong>> GetEnumerator()
            {
                Sort();
                return dict.GetEnumerator();
            }

            public bool Remove(ulong key)
            {
                return dict.RemoveAll(e => e.Key == key) > 0;
            }

            public bool Remove(KeyValuePair<ulong, ulong> item)
            {
                return dict.Remove(item);
            }

            public void Set(UserAccount user)
            {
                var val = func.Invoke(user);
                if (val > 0)
                {
                    if (ContainsKey(user.ID))
                    {
                        this[user.ID] = val;
                    }
                    else
                    {
                        Add(user.ID, val);
                    }
                }
            }

            public bool TryGetValue(ulong key, [MaybeNullWhen(false)] out ulong value)
            {
                try
                {
                    if (ContainsKey(key))
                    {
                        value = this[key];
                        return true;
                    }
                    else
                    {
                        value = 0;
                        return false;
                    }
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
                return ((IEnumerable)dict).GetEnumerator();
            }
        }
    }
}