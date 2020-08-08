using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace IodemBot.Core
{
    public static class DataStorage
    {
        private static bool isSaving = false;

        //Save All userAccounts
        public static void SaveUserAccounts<T>(IEnumerable<T> accounts, string filePath)
        {
            try
            {
                //prevent crashes.
                if (isSaving)
                {
                    return;
                }

                isSaving = true;
                string json = JsonConvert.SerializeObject(accounts, Formatting.Indented);
                if (json.Length < 5)
                {
                    throw new JsonException($"Length of json string appears to be corrupted: {json.Length}. Aborting Saving.");
                }
                File.WriteAllText(filePath, json);
                isSaving = false;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while saving:" + e.ToString());
            }
        }

        //Get All userAccounts
        public static IEnumerable<T> LoadListFromFile<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<List<T>>(json);
        }

        public static bool SaveExists(string filePath)
        {
            return File.Exists(filePath);
        }
    }
}