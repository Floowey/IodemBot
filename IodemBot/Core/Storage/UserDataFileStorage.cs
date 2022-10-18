using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.Caching;
using IodemBot.Core.UserManagement;
using Newtonsoft.Json;

namespace IodemBot.Core
{
    internal class UserDataFileStorage : IPersistentStorage<UserAccount>
    {
        private const string FolderPath = "Resources/Accounts/AccountFiles";
        private const string BackupPath = "Resources/Accounts/BackupAccountFiles";
        private static readonly ConcurrentDictionary<ulong, object> Locks = new();
        private static readonly MemoryCache Cache = MemoryCache.Default;

        private static readonly object Locklock = new();

        static UserDataFileStorage()
        {
            if (!File.Exists(FolderPath)) Directory.CreateDirectory(FolderPath);

            if (!File.Exists(BackupPath)) Directory.CreateDirectory(BackupPath);
        }

        public bool Exists(ulong id)
        {
            return File.Exists(Path.Combine(FolderPath, $"{id}.json"));
        }

        public void Remove(ulong id)
        {
            File.Delete(Path.Combine(FolderPath, $"{id}.json"));
        }

        public IEnumerable<UserAccount> RestoreAll()
        {
            foreach (var f in Directory.GetFiles(FolderPath, "*.json"))
            {
                var idstring = Path.GetFileNameWithoutExtension(f);
                if (ulong.TryParse(idstring, out var id))
                    yield return RestoreSingle(id, false);
                else
                    Console.WriteLine($"File {f} could not be loaded.");
            }
        }

        public UserAccount RestoreSingle(ulong id)
        {
            try
            {
                var lockobj = GetLock(id);
                lock (lockobj)
                {
                    var user = Cache[$"{id}_user"] as UserAccount;
                    if (user == null)
                    {
                        var filePath = Path.Combine(FolderPath, $"{id}.json");
                        var backupFile = Path.Combine(BackupPath, $"{id}.json");
                        if (!File.Exists(filePath))
                        {
                            if (!File.Exists(backupFile))
                            {
                                Console.WriteLine($"User not registered: {id}");
                                return null;
                            }

                            Console.WriteLine("Main File not found, using backup");
                            filePath = backupFile;
                        }

                        try
                        {
                            var json = File.ReadAllText(filePath);
                            user = JsonConvert.DeserializeObject<UserAccount>(json);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Reading file failed, trying backup: {e}");
                            var json = File.ReadAllText(backupFile);
                            user = JsonConvert.DeserializeObject<UserAccount>(json);
                        }
                    }

                    return user;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Loading user {id} critically failed: {e}");
                return null;
            }
        }

        public void Store(UserAccount item)
        {
            lock (GetLock(item.Id))
            {
                EnsureFiles(item.Id);
                var json = JsonConvert.SerializeObject(item, Formatting.Indented);

                var cacheHash = (int)(Cache.Get($"{item.Id}_hash") ?? 0);
                var newHash = json.GetHashCode();
                if (newHash == cacheHash) return;

                File.Replace(Path.Combine(FolderPath, $"{item.Id}.json"), Path.Combine(BackupPath, $"{item.Id}.json"),
                    Path.Combine(BackupPath, $"{item.Id}_B.json"));
                File.WriteAllText(Path.Combine(FolderPath, $"{item.Id}.json"), json);

                Cache.Set($"{item.Id}_hash", newHash,
                    new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(30) });
            }
        }

        public void Update(UserAccount item)
        {
            Store(item);
        }

        public bool Exists(Expression<Func<UserAccount, bool>> predicate)
        {
            throw new NotImplementedException();
        }

        public void Remove(Expression<Func<UserAccount, bool>> predicate)
        {
            throw new NotImplementedException();
        }

        public UserAccount RestoreSingle(Expression<Func<UserAccount, bool>> predicate)
        {
            throw new NotImplementedException();
        }

        private static object GetLock(ulong id)
        {
            lock (Locklock)
            {
                var datalock = new object();
                return Locks.GetOrAdd(id, datalock);
            }
        }

        public UserAccount RestoreSingle(ulong id, bool doCache)
        {
            var user = RestoreSingle(id);
            if (doCache)
                Cache.Set($"{id}_user", user, new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes(10) });
            return user;
        }

        private static void EnsureFiles(ulong id)
        {
            if (!File.Exists(Path.Combine(FolderPath, $"{id}.json")))
                File.Create(Path.Combine(FolderPath, $"{id}.json")).Close();

            if (!File.Exists(Path.Combine(BackupPath, $"{id}.json")))
                File.Create(Path.Combine(BackupPath, $"{id}.json")).Close();

            if (!File.Exists(Path.Combine(BackupPath, $"{id}_B.json")))
                File.Create(Path.Combine(BackupPath, $"{id}_B.json")).Close();
        }
    }
}