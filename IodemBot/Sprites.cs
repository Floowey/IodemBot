using System.Collections.Generic;
using System.IO;
using System.Linq;
using IodemBot.Extensions;
using Newtonsoft.Json;

namespace IodemBot
{
    public class Sprites
    {
        private static readonly Dictionary<string, string> sprites;

        public static string GetImageFromName(string name)
        {
            name = name.ToLower();
            if (sprites.ContainsKey(name))
            {
                return sprites[name];
            }

            return sprites["unknown"];
        }

        static Sprites()
        {
            string json = File.ReadAllText("SystemLang/sprites.json");
            var data = JsonConvert.DeserializeObject<dynamic>(json);
            sprites = data.ToObject<Dictionary<string, string>>();
        }

        public static int GetSpriteCount()
        {
            return sprites.Count;
        }

        public static string GetRandomSprite()
        {
            return sprites.Random().Value;
        }
    }
}