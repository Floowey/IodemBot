using System.Collections.Generic;
using System.Linq;
using Discord;

namespace IodemBot
{
    public static class Colors
    {
        private static readonly Dictionary<string, Color> ColorsDictionary;

        static Colors()
        {
            ColorsDictionary = new Dictionary<string, Color>
            {
                {"Iodem", new Color(133, 63, 177)},
                {"Venus", new Color(227, 167, 63)},
                {"Mars", new Color(0xdd2e44)},
                {"Jupiter", new Color(0xaa5dca)},
                {"Mercury", new Color(0x6495ed)},
                {"Exathi", new Color(0xc5c5d6)},
                {"none", new Color(0xc5c5d6)},
                {"Shop", new Color(0xf8f800)},
                {"Artifact", new Color(0xffe493)},
                {"Error", new Color(0x821f01)}
            };
        }

        public static Color Get(string key)
        {
            if (ColorsDictionary.ContainsKey(key)) return ColorsDictionary[key];

            return new Color(255, 255, 255);
        }

        public static Color Get(IEnumerable<string> keys)
        {
            var enumerable = keys.ToList();
            if (!enumerable.Any()) return Color.LightGrey;
            var r = 0;
            var g = 0;
            var b = 0;

            foreach (var c in enumerable)
            {
                var col = Get(c);
                r += col.R;
                g += col.G;
                b += col.B;
            }

            return new Color(r / enumerable.Count, g / enumerable.Count, b / enumerable.Count);
        }
    }
}