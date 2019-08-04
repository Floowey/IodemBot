using Discord;
using System.Linq;

namespace IodemBot.Extensions
{
    public static class EmbedBuilderExtension
    {
        public static bool AllFieldsEqual(this EmbedBuilder builder, EmbedBuilder other)
        {
            if (builder.Length != other.Length)
            {
                return false;
            }

            if (builder.Fields.Count != other.Fields.Count)
            {
                return false;
            }

            if (!builder.Fields.Select(f => f.Name).All(thisName => other.Fields.Select(k => k.Name).Any(otherName => otherName.Equals(thisName))))
            {
                return false;
            }
            if (!builder.Fields.Select(f => f.Value).All(thisName => other.Fields.Select(k => k.Value).Any(otherName => otherName.Equals(thisName))))
            {
                return false;
            }
            return true;
        }
    }
}