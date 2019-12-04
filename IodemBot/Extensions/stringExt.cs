namespace IodemBot.Extensions
{
    public static class StringExt
    {
        public static bool IsNullOrEmpty(this string s)
        {
            return (s == null || s == "") ? true : false;
        }
    }
}