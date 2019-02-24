using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Discord;
using Discord.Commands;
using IodemBot.Modules;

namespace IodemBot
{
    public class Sprites
    {
        private static Dictionary<string, string> sprites;
        public static string GetImageFromName(string name)
        {
            name = name.ToLower();
            if(sprites.ContainsKey(name)) return sprites[name];
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
            int r = (new Random()).Next(0, sprites.Count);
            return sprites.ElementAt(r).Value;
        }
    }
}
