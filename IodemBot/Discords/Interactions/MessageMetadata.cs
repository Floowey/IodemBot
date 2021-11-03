using System;
using System.IO;
using Discord;

namespace IodemBot.Discords
{
    public class MessageMetadata
    {
        public MessageMetadata(Embed embed, Stream imageStream, bool imageIsSpoiler, string imageFileName,
            string message, bool success, bool hasMentions, ComponentBuilder componentBuilder)
        {
            Embed = embed;
            ImageStream = imageStream;
            ImageIsSpoiler = imageIsSpoiler;
            ImageFileName = imageFileName;
            Message = message;
            Success = success;
            HasMentions = hasMentions;
            Components = componentBuilder?.Build();
        }

        public Embed Embed { get; set; }
        public Stream ImageStream { get; set; }
        public bool ImageIsSpoiler { get; set; }
        public string ImageFileName { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
        public bool HasMentions { get; set; }
        public MessageComponent Components { get; set; }

        internal object RandomElement()
        {
            throw new NotImplementedException();
        }
    }
}