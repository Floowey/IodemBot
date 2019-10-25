using Newtonsoft.Json;
using System.IO;

namespace IodemBot
{
    internal class TwitchConfig
    {
        private const string configFolder = "Resources";
        private const string configFile = "twitch.json";

        public static TwitchBotConfig bot;

        static TwitchConfig()
        {
            if (!Directory.Exists(configFolder))
            {
                Directory.CreateDirectory(configFolder);
            }

            if (!File.Exists(configFolder + "/" + configFile))
            {
                bot = new TwitchBotConfig();
                string json = JsonConvert.SerializeObject(bot, Formatting.Indented);
                File.WriteAllText(configFolder + "/" + configFile, json);
            }
            else
            {
                string json = File.ReadAllText(configFolder + "/" + configFile);
                bot = JsonConvert.DeserializeObject<TwitchBotConfig>(json);
            }
        }
    }

    public struct TwitchBotConfig
    {
        public string clientID;
        public string clientSecret;
    }
}