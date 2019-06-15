using IodemBot.Core.UserManagement;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace IodemBot.Core
{
    public static class DataStorage
    {
        private static bool isSaving = false;

        //Save All userAccounts
        public static void SaveUserAccounts(IEnumerable<UserAccount> accounts, string filePath)
        {
            //prevent crashes.
            if (isSaving)
            {
                return;
            }

            isSaving = true;
            string json = JsonConvert.SerializeObject(accounts, Formatting.Indented);
            File.WriteAllText(filePath, json);
            isSaving = false;
        }

        //Get All userAccounts
        public static IEnumerable<UserAccount> LoadUserAccounts(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<List<UserAccount>>(json);
        }

        public static bool SaveExists(string filePath)
        {
            return File.Exists(filePath);
        }
    }
}