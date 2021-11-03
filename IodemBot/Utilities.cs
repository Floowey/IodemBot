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
    }
}