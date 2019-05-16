using Discord.WebSocket;
using System;

namespace IodemBot
{
    internal static class Global
    {
        internal static DiscordSocketClient Client { get; set; }
        internal static ulong MessageIdToTrack { get; set; }
        internal static Random Random { get; set; } = new Random();
        internal static DateTime UpSince { get; set; }
    }
}