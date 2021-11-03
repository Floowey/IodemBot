using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace IodemBot.Modules
{
    public class Quotes
    {
        internal static readonly List<QuoteStruct> quoteList = new List<QuoteStruct>();

        public static void AddQuote(string name, string quote)
        {
            quoteList.Add(new QuoteStruct(name.ToLower(), quote));
            SaveData();
        }

        public static int GetQuotesCount()
        {
            return quoteList.Count;
        }

        static Quotes()
        {
            // Load data
            if (!ValidateStorageFile("SystemLang/quotes.json"))
            {
                return;
            }

            string json = File.ReadAllText("SystemLang/quotes.json");
            quoteList = JsonConvert.DeserializeObject<List<QuoteStruct>>(json);
        }

        public static void SaveData()
        {
            // Save data
            string json = JsonConvert.SerializeObject(quoteList, Formatting.Indented);
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
            public string name;
            public string quote;

            public QuoteStruct(string name, string quote)
            {
                this.name = name;
                this.quote = quote;
            }
        }
    }
}