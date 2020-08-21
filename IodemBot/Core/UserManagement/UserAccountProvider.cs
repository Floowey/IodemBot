using System.Collections.Generic;

namespace IodemBot.Core.UserManagement
{
    public static class UserAccountProvider
    {
        private static readonly PersistentStorage _persistentStorage;

        static UserAccountProvider()
        {
            _persistentStorage = new PersistentStorage();
        }

        public static UserAccount GetById(ulong userId)
        {
            var user = _persistentStorage.RestoreSingle<UserAccount>(u => u.ID == userId);
            return EnsureExists(user, userId);
        }

        public static void StoreUser(UserAccount user)
        {
            if (_persistentStorage.Exists<UserAccount>(u => u.ID == user.ID))
            {
                _persistentStorage.Update(user);
            }
            else
            {
                _persistentStorage.Store(user);
            }
        }

        public static void RemoveUser(UserAccount user)
        {
            if (_persistentStorage.Exists<UserAccount>(u => u.ID == user.ID))
            {
                _persistentStorage.Remove<UserAccount>(u => u.ID == user.ID);
            }
        }

        public static IEnumerable<UserAccount> GetAllUsers()
            => _persistentStorage.RestoreAll<UserAccount>();

        private static UserAccount EnsureExists(UserAccount user, ulong userId)
        {
            if (user is null)
            {
                user = UserAccounts.GetAccount(userId);
                StoreUser(user);
            }
            return user;
        }
    }
}