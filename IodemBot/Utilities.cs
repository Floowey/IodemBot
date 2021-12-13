using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IodemBot
{
    internal static class Utilities
    {
        public static string ToCaps(string input)
        {
            var words = input.Split(' ');
            var output = "";

            foreach (var word in words)
            {
                var tmpString = word.ToLower();

                var wordAsArray = tmpString.ToCharArray();

                wordAsArray[0] = char.ToUpper(wordAsArray[0]);

                output += $"{new string(wordAsArray)} ";
            }

            return output.Trim();
        }

        //private static List<(double value, string emote)> progressBarEmotes = new()
        //{
        //    (0, "<:Bar0:909126366820200469>"),
        //    (12.5, "<:Bar12:909126366790828062>"),
        //    (25, "<:Bar25:909126366815998022>"),
        //    (37.5, "<:Bar37:909126366874722354>"),
        //    (50, "<:Bar50:909126367126368296>"),
        //    (62.5, "<:Bar62:909126367097020416>"),
        //    (75, "<:Bar75:909126367239622686>"),
        //    (87.5, "<:Bar87:909126367189286952>"),
        //    (100, "<:Bar100:909126367034114049>")
        //};
        private static List<(double value, string emote)> progressBarEmotes = new()
        {
            (0, "<:bar8:909741755350523975>"),
            (12.5, "<:bar7:909741755354714182>"),
            (25, "<:bar6:909741755547668501>"),
            (37.5, "<:bar5:909741755329552444>"),
            (50, "<:bar4:909741755103076403>"),
            (62.5, "<:bar3:909741755283415040>"),
            (75, "<:bar2:909741755312783360>"),
            (87.5, "<:bar1:909741755337965568>"),
            (100, "<:bar0:909741755300188202>")
        };

        public static string GetProgressBar(int percent, int length = 5)
        {
            List<string> s = new();
            var oneCell = (float)100 / length;
            for (int i = 0; i < percent / oneCell - 1; i++)
                s.Add(progressBarEmotes.Last().emote);
            if (percent % oneCell > 0.1 || percent == 0)
                s.Add(progressBarEmotes.Last(v => (percent % oneCell) / oneCell * 100 >= v.value).emote);
            else
                s.Add(progressBarEmotes.Last().emote);

            while (s.Count < length)
                s.Add(progressBarEmotes.First().emote);
            return string.Join("", s);
        }
    }
}