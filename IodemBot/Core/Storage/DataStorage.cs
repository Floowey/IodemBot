using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace IodemBot.Core
{
    public static class DataStorage
    {
        private static bool _isSaving;
        private static readonly object DataLock = new();

        //Save All userAccounts
        public static void SaveUserAccounts<T>(IEnumerable<T> accounts, string filePath)
        {
            try
            {
                if (_isSaving) return;

                lock (DataLock)
                {
                    _isSaving = true;
                    var json = JsonConvert.SerializeObject(accounts, Formatting.Indented);
                    if (json.Length < 5)
                        throw new JsonException(
                            $"Length of json string appears to be corrupted: {json.Length}. Aborting Saving.");
                    File.WriteAllText(filePath, json);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while saving:" + e);
            }
            finally
            {
                _isSaving = false;
            }
        }

        //Get All userAccounts
        public static IEnumerable<T> LoadListFromFile<T>(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<List<T>>(json);
        }

        public static bool SaveExists(string filePath)
        {
            return File.Exists(filePath);
        }
    }
}