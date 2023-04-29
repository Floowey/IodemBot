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

        public static string Article(string s)
        {
            s = s.ToLower();
            char c = s.ElementAt(0);
            return c switch
            {
                'a' or 'e' or 'i' or 'o' or 'u' => "an",
                _ => "a",
            };
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

        public static readonly Dictionary<string, List<(double value, string emote)>> progressBars = new(){
            { "classic", new(){
            (0, "<:bar8:909741755350523975>"),
            (12.5, "<:bar7:909741755354714182>"),
            (25, "<:bar6:909741755547668501>"),
            (37.5, "<:bar5:909741755329552444>"),
            (50, "<:bar4:909741755103076403>"),
            (62.5, "<:bar3:909741755283415040>"),
            (75, "<:bar2:909741755312783360>"),
            (87.5, "<:bar1:909741755337965568>"),
            (100, "<:bar0:909741755300188202>")}
            },
            { "gba", new(){
            (0, "<:Bar0:909126366820200469>"),
            (12.5, "<:Bar12:909126366790828062>"),
            (25, "<:Bar25:909126366815998022>"),
            (37.5, "<:Bar37:909126366874722354>"),
            (50, "<:Bar50:909126367126368296>"),
            (62.5, "<:Bar62:909126367097020416>"),
            (75, "<:Bar75:909126367239622686>"),
            (87.5, "<:Bar87:909126367189286952>"),
            (100, "<:Bar100:909126367034114049>") }
            },
            { "red", new(){
            (0, "<:bars43:1099654608739123292>"),
            (12.5, "<:bars08:1099621853057781760>"),
            (25, "<:bars07:1099621851346501803>"),
            (37.5, "<:bars06:1099621848834121781>"),
            (50, "<:bars05:1099621847793946775>"),
            (62.5, "<:bars04:1099621846384656424>"),
            (75, "<:bars03:1099621844358795384>"),
            (87.5, "<:bars02:1099621843025002568>"),
            (100, "<:bars01:1099621841850605608>") }
            },
            { "yellow", new(){
            (0, "<:bars43:1099654608739123292>"),
            (12.5, "<:bars16:1099622520623222794>"),
            (25, "<:bars15:1099622519222325318>"),
            (37.5, "<:bars14:1099622517230022747>"),
            (50, "<:bars13:1099622515866878002>"),
            (62.5, "<:bars12:1099622513568387142>"),
            (75, "<:bars11:1099622512213639218>"),
            (87.5, "<:bars10:1099622510892421120>"),
            (100, "<:bars09:1099622508656865340>") }
            },
            { "purple", new(){
            (0, "<:bars43:1099654608739123292>"),
            (12.5, "<:bars24:1099622808918696016>"),
            (25, "<:bars23:1099622807551361075>"),
            (37.5, "<:bars22:1099622804648894544>"),
            (50, "<:bars21:1099622803113791558>"),
            (62.5, "<:bars20:1099622799745744938>"),
            (75, "<:bars19:1099622526319079478>"),
            (87.5, "<:bars18:1099622525211791450>"),
            (100, "<:bars17:1099622522401587250>") }
            },
            { "green", new(){
            (0, "<:bars43:1099654608739123292>"),
            (12.5, "<:bars32:1099654520205750272>"),
            (25, "<:bars31:1099654516825141339>"),
            (37.5, "<:bars30:1099622820822130720>"),
            (50, "<:bars29:1099622818313928835>"),
            (62.5, "<:bars28:1099622816531349535>"),
            (75, "<:bars27:1099622814446784573>"),
            (87.5, "<:bars26:1099622812936847461>"),
            (100, "<:bars25:1099622811384942643>") }
            },
            { "blue", new(){
            (0, "<:bars43:1099654608739123292>"),
            (12.5, "<:bbar0:1100076283439882280>"),
            (25, "<:bbar1:1100076285910335648>"),
            (37.5, "<:bbar2:1100076287969738874>"),
            (50, "<:bbar3:1100076290117206117>"),
            (62.5, "<:bbar4:1100076291765588141>"),
            (75, "<:bbar5:1100076293380378814>"),
            (87.5, "<:bbar6:1100076295729205370>"),
            (100, "<:bars00:1100076481671090227>") }
            },
            { "baguette", new(){
            (0, "<:bars43:1099654608739123292>"),
            (11.1, "<:bars42:1099654537352052807>"),
            (22.2, "<:bars41:1099654535787597845>"),
            (33.3, "<:bars40:1099654534097281107>"),
            (44.4, "<:bars39:1099654533245833276>"),
            (55.5, "<:bars38:1099654530972516422>"),
            (66.6, "<:bars37:1099654529231900793>"),
            (77.7, "<:bars36:1099654528271401052>"),
            (88.8, "<:bars35:1099654526161658026>"),
            (100, "<:bars34:1099654524874006559>") }
            }
        };

        public static readonly string DefaultBar = "classic";

        public static string GetProgressBar(int percent, int length = 5, string theme = "classic")
        {
            if (!progressBars.ContainsKey(theme))
                theme = progressBars.Keys.First();

            var progressBarEmotes = progressBars[theme];

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