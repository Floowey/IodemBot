using Discord;

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
        public EmbedMetadata(string title, Color color, string thumbnailUrl)
        {
            EmbedTitle = title;
            EmbedColor = color;
            EmbedThumbnailUrl = thumbnailUrl;
        }

        public string EmbedTitle { get; }
        public Color EmbedColor { get; }
        public string EmbedThumbnailUrl { get; }
    }
}