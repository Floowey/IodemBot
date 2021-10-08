using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Discords
{
    public interface IEmbedMetadata
    {
        string EmbedTitle { get; }
        Color EmbedColor { get; }
        string EmbedThumbnailUrl { get; }
    }

    public class EmbedMetadata : IEmbedMetadata
    {
        public string EmbedTitle { get; }
        public Color EmbedColor { get; }
        public string EmbedThumbnailUrl { get; }

        public EmbedMetadata(string title, Color color, string thumbnailUrl)
        {
            EmbedTitle = title;
            EmbedColor = color;
            EmbedThumbnailUrl = thumbnailUrl;
        }
    }
}
