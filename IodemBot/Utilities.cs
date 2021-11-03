using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace IodemBot
{
    internal class Utilities
    {
        private static readonly Dictionary<string, string> alerts;

        static Utilities()
        {
            string json = File.ReadAllText("SystemLang/alerts.json");
            var data = JsonConvert.DeserializeObject<dynamic>(json);
            alerts = data.ToObject<Dictionary<string, string>>();
        }


        public static string GetFormattedAlert(string key, params object[] parameter)
        {
            if (alerts.ContainsKey(key))
            {
                return String.Format(alerts[key], parameter);
            }
            return "";
        }

        public static string ToCaps(string input)
        {
            string[] words = input.Split(' ');
            string output = "";

            foreach (string word in words)
            {
                string tmpString = word.ToLower();

                char[] wordAsArray = tmpString.ToCharArray();

                wordAsArray[0] = char.ToUpper(wordAsArray[0]);

                output += $"{new string(wordAsArray)} ";
            }

            return output.Trim();
        }
    }
}