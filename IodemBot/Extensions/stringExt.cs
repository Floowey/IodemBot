using System.Text.RegularExpressions;

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
            return Regex.Replace(s, "[\"\n@,$\\$]", "");
        }
    }
}