using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using IodemBot.Core.UserManagement;
using Newtonsoft.Json;

namespace IodemBot.Core
{
    class UserDataFileStorage : IPersistentStorage<UserAccount>
    {
        private static readonly string FolderPath = "Resources/Accounts/AccountFiles";
        private static readonly string BackupPath = "Resources/Accounts/BackupAccountFiles";
        private static readonly ConcurrentDictionary<ulong, object> locks = new ConcurrentDictionary<ulong, object>();
        private static readonly MemoryCache cache = MemoryCache.Default;

        static UserDataFileStorage()
        {
            if (!File.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
            }

            if (!File.Exists(BackupPath))
            {
                Directory.CreateDirectory(BackupPath);
            }
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
                if (ulong.TryParse(idstring, out ulong id))
                {
                    yield return RestoreSingle(id, false);
                } else
                {
                    Console.WriteLine($"File {f} could not be loaded.");
                }
            }
        }
        private static readonly object locklock = new object();
        private static object GetLock(ulong id)
        {
            lock (locklock)
            {
                object datalock = new object();
                return locks.GetOrAdd(id, datalock);
            }
        }
        public UserAccount RestoreSingle(ulong id, bool doCache)
        {
            var user = RestoreSingle(id);
            if (doCache)
            {
                cache.Set($"{id}_user", user, new CacheItemPolicy() { SlidingExpiration = TimeSpan.FromMinutes(10) });
            }
            return user;
        }
        public UserAccount RestoreSingle(ulong id)
        {
            try
            {
                object lockobj = GetLock(id);
                lock (lockobj)
                {
                    Console.WriteLine($"{id} ({lockobj.GetHashCode()}) entered on thread {Thread.CurrentThread.ManagedThreadId}");
                    UserAccount user = cache[$"{id}_user"] as UserAccount;
                    if(user == null)
                    {
                        var filePath = Path.Combine(FolderPath, $"{id}.json");
                        var backupFile = Path.Combine(BackupPath, $"{id}.json");
                        if (!File.Exists(filePath))
                        {
                            if (!File.Exists(backupFile))
                            {
                                Console.WriteLine($"User not registered: {id}");
                                return null;
                            } else
                            {
                                Console.WriteLine($"Main File not found, using backup");
                                filePath = backupFile;
                            }
                        }

                        try
                        {
                            var json = File.ReadAllText(filePath);
                            user = JsonConvert.DeserializeObject<UserAccount>(json);
                        } catch
                        {
                            Console.WriteLine($"Reading file failed, trying backup");
                            var json = File.ReadAllText(backupFile);
                            user = JsonConvert.DeserializeObject<UserAccount>(json);
                        }
                    }
                    Console.WriteLine($"{id} ({lockobj.GetHashCode()}) exits on thread {Thread.CurrentThread.ManagedThreadId}");
                    return user;
                }       
            } catch (Exception e) {
                Console.WriteLine($"Loading user {id} critically failed: {e}");
                return null;
            }
        }
        public void Store(UserAccount item)
        {
            lock (GetLock(item.ID))
            {
                EnsureFiles(item.ID);
                string json = JsonConvert.SerializeObject(item, Formatting.Indented);

                int cacheHash = (int)(cache.Get($"{item.ID}_hash") ?? 0);
                int newHash = json.GetHashCode();
                if (newHash == cacheHash)
                {
                    return;
                }
                
                File.Replace(Path.Combine(FolderPath, $"{item.ID}.json"), Path.Combine(BackupPath, $"{item.ID}.json"), Path.Combine(BackupPath, $"{item.ID}_B.json"));
                File.WriteAllText(Path.Combine(FolderPath, $"{item.ID}.json"), json);

                cache.Set($"{item.ID}_hash", newHash, new CacheItemPolicy() { AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(30) });
            }
        }

        private static void EnsureFiles(ulong id)
        {
            if(!File.Exists(Path.Combine(FolderPath, $"{id}.json")))
            {
                File.Create(Path.Combine(FolderPath, $"{id}.json")).Close();
            }

            if (!File.Exists(Path.Combine(BackupPath, $"{id}.json")))
            {
                File.Create(Path.Combine(BackupPath, $"{id}.json")).Close();
            }

            if (!File.Exists(Path.Combine(BackupPath, $"{id}_B.json")))
            {
                File.Create(Path.Combine(BackupPath, $"{id}_B.json")).Close();
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
    }
}
