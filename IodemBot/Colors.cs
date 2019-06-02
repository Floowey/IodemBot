using Discord;
using System.Collections.Generic;

namespace IodemBot
{
    public static class Colors
    {
        private static Dictionary<string, Color> colors;

        static Colors()
        {
            colors = new Dictionary<string, Color>
            {
                { "Iodem", new Color(133, 63, 177) },
                { "Venus", new Color(227, 167, 63) },
                { "Mars", new Color(179, 10, 0) },
                { "Jupiter", new Color(166, 106, 207) },
                { "Mercury", new Color(100, 149, 237) },
                { "Exathi", new Color(0xc5c5d6)},
                { "Shop", new Color(0xf8f800)},
                { "Artifact", new Color(0xffe493)},
                { "Error", new Color(0x821f01)}
            };
        }

        public static Color Get(string key)
        {
            if (colors.ContainsKey(key))
            {
                return colors[key];
            }

            return new Color(255, 255, 255);
        }

        public static Color Get(string[] keys)
        {
            int r = 0;
            int g = 0;
            int b = 0;
            foreach (string c in keys)
            {
                var col = Get(c);
                r += col.R;
                g += col.G;
                b += col.B;
            }
            return new Color(r / keys.Length, g / keys.Length, b / keys.Length);
        }
    }
}