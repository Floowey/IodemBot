using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace IodemBot.Modules
{
    public class Quotes
    {
        internal static readonly List<QuoteStruct> QuoteList = new();

        static Quotes()
        {
            // Load data
            if (!ValidateStorageFile("SystemLang/quotes.json")) return;

            var json = File.ReadAllText("SystemLang/quotes.json");
            QuoteList = JsonConvert.DeserializeObject<List<QuoteStruct>>(json);
        }

        public static void AddQuote(string name, string quote)
        {
            QuoteList.Add(new QuoteStruct(name.ToLower(), quote));
            SaveData();
        }

        public static int GetQuotesCount()
        {
            return QuoteList.Count;
        }

        public static void SaveData()
        {
            // Save data
            var json = JsonConvert.SerializeObject(QuoteList, Formatting.Indented);
            File.WriteAllText("SystemLang/quotes.json", json);
        }

        private static bool ValidateStorageFile(string file)
        {
            if (!File.Exists(file))
            {
                File.WriteAllText(file, "");
                SaveData();
                return false;
            }

            return true;
        }

        internal struct QuoteStruct
        {
            public string Name;
            public string Quote;

            public QuoteStruct(string name, string quote)
            {
                Name = name;
                Quote = quote;
            }
        }
    }
}