using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;

namespace IodemBot
{
    public static class ListEmbedFieldBuilderExtensions
    {
        public static void Add(this List<EmbedFieldBuilder> fieldEmbed, string name, string content, bool appendIfTooLong = true, bool isInline = false)
        {
            bool tooLong = false;
            if (content.Length > 1024)
            {
                tooLong = true;
                content = content.Substring(0, 1021) + "...";
            }

            fieldEmbed.Add(new EmbedFieldBuilder()
            {
                Name = name == null ? null : name + (appendIfTooLong && tooLong ? $" (truncated)" : ""),
                Value = content,
                IsInline = isInline
            });
        }

        public static void Add(this List<EmbedFieldBuilder> fieldEmbed, string name, IEnumerable<string> rows, bool appendIfTooLong = true, bool isInline = false)
        {
            bool tooLong = false;
            int rowsUsed = 0;
            StringBuilder sb = new StringBuilder();
            foreach (var row in rows)
            {
                if (sb.Length + Environment.NewLine.Length + row.Length > 1024)
                {
                    tooLong = true;
                    break;
                }

                if (sb.Length != 0)
                    sb.Append(Environment.NewLine);

                sb.Append(row);
                rowsUsed++;
            }

            fieldEmbed.Add(new EmbedFieldBuilder()
            {
                Name = name == null ? null : name + (appendIfTooLong && tooLong ? $" (+ {rows.Count() - rowsUsed} more)" : ""),
                Value = sb.ToString(),
                IsInline = isInline
            });
        }
    }
}