using Discord.WebSocket;
using System;

namespace IodemBot
{
    internal static class Global
    {
        public static DateTime RunningSince { get; internal set; }
        internal static DiscordSocketClient Client { get; set; }
        internal static ulong MessageIdToTrack { get; set; }
        internal static Random Random { get; set; } = new Random();
        internal static DateTime UpSince { get; set; }
        internal static ulong MainChannel = 355558866282348574;

        internal static string DateString
        {
            get
            {
                return DateTime.Now.ToString("MM_dd_HH-mm-ss");
            }
        }
    }
}