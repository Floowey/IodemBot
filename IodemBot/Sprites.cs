using System.Collections.Generic;
using System.IO;
using IodemBot.Extensions;
using Newtonsoft.Json;

namespace IodemBot
{
    public class Sprites
    {
        private static readonly Dictionary<string, string> SpritesDictionary;

        public static string GetImageFromName(string name)
        {
            name = name.ToLower();
            if (SpritesDictionary.ContainsKey(name))
            {
                return SpritesDictionary[name];
            }

            return SpritesDictionary["unknown"];
        }

        static Sprites()
        {
            string json = File.ReadAllText("SystemLang/Sprites.json");
            var data = JsonConvert.DeserializeObject<dynamic>(json);
            SpritesDictionary = data.ToObject<Dictionary<string, string>>();
        }

        public static int GetSpriteCount()
        {
            return SpritesDictionary.Count;
        }

        public static string GetRandomSprite()
        {
            return SpritesDictionary.Random().Value;
        }
    }
}