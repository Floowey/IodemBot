using System;
using Discord;
using Discord.WebSocket;

namespace IodemBot
{
    internal static class Global
    {
        public static DateTime RunningSince { get; internal set; }
        internal static DiscordSocketClient Client { get; set; }
        internal static ulong MessageIdToTrack { get; set; }
        public static Random Random { get; set; } = new Random();

        private static readonly object synclock = new object();
        internal static int RandomNumber(int low, int high)
        {
            lock (synclock)
            {
                return Random.Next(low, high);
            }
        }
        internal static DateTime UpSince { get; set; }

        internal static IUser Owner
        {
            get
            {
                if (_Owner is null)
                {
                    _Owner = Client.GetApplicationInfoAsync().Result.Owner;
                }
                return _Owner;
            }
        }
        private static IUser _Owner { get; set; }

        internal static string DateString
        {
            get
            {
                return $"{DateTime.Now:s}".Replace(":", ".");
            }
        }
    }
}