using System.IO;
using Newtonsoft.Json;

namespace IodemBot
{
    internal static class Config
    {
        private const string ConfigFolder = "Resources";
        private const string ConfigFile = "config.json";

        public static BotConfig Bot;

        static Config()
        {
            if (!Directory.Exists(ConfigFolder)) Directory.CreateDirectory(ConfigFolder);

            if (!File.Exists(ConfigFolder + "/" + ConfigFile))
            {
                Bot = new BotConfig();
                var json = JsonConvert.SerializeObject(Bot, Formatting.Indented);
                File.WriteAllText(ConfigFolder + "/" + ConfigFile, json);
            }
            else
            {
                var json = File.ReadAllText(ConfigFolder + "/" + ConfigFile);
                Bot = JsonConvert.DeserializeObject<BotConfig>(json);
            }
        }
    }

    public struct BotConfig
    {
        public string Token;
        public string CmdPrefix;
    }
}