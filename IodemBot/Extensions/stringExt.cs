using System.Text.RegularExpressions;
using System;
using Discord;

namespace IodemBot.Extensions
{
    public static class StringExt
    {
        public static bool IsNullOrEmpty(this string s)
        {
            return string.IsNullOrEmpty(s);
        }

        public static string RemoveBadChars(this string s)
        {
            s ??= "";
            s = Regex.Replace(s, "[\"\n@,$\\$]", "");
            s = Regex.Replace(s, "_", @"\_");
            s = Regex.Replace(s, @"\*", @"\\\*");

            return s;
        }

        /// <summary>
        /// Returns a shortened form a given Emote. e.g. "<:isaac_fallen:490017761972715530>" to "<:i:490017761972715530>".
        /// If the string is an Emoji, e.g. :grimasse: it returns it as is.
        /// Otherwise raises ArgumentException if invalid string.
        /// </summary>
        /// <param name="s"></param>
        /// <returns>The shortened form of the emote.</returns>
        public static string ToShortEmote(this string s)
        {
            if (!(Emote.TryParse(s, out _) || Emoji.TryParse(s, out _)))
                throw new ArgumentException($"{s} not a valid Emote sequence");

            var emote = Regex.Match(s, @"<(a?):(.+):(\d{18})>");
            if (!emote.Success)
            {
                return s;
            }
            return $"<{emote.Groups[1].Value}:{emote.Groups[2].Value[0]}:{emote.Groups[3].Value}>";
            //return m.Groups[0].Value;
        }
    }
}