using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json;
using System.IO;

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
                { "Mercury", new Color(100, 149, 237) }
            };
        }

        public static Color get(string key)
        {
            if (colors.ContainsKey(key)) return colors[key];
            return new Color(255, 255, 255);
        }

    }
}
