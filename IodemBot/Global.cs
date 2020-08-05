using System;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IodemBot
{
    internal static class Global
    {
        public static DateTime RunningSince { get; internal set; }
        internal static DiscordSocketClient Client { get; set; }
        internal static ulong MessageIdToTrack { get; set; }
        internal static Random Random { get; set; } = new Random();
        internal static DateTime UpSince { get; set; }

        internal static string DateString
        {
            get
            {
                return $"{DateTime.Now:s}".Replace(":", ".");
            }
        }
    }
}