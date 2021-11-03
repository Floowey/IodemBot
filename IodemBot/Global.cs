using System;
using Discord;
using Discord.WebSocket;

namespace IodemBot
{
    internal static class Global
    {
        private static readonly object Synclock = new();
        public static DateTime RunningSince { get; internal set; }
        internal static DiscordSocketClient Client { get; set; }
        private static Random Random { get; set; } = new();

        internal static DateTime UpSince { get; set; }

        internal static IUser Owner => _owner ??= Client.GetApplicationInfoAsync().Result.Owner;

        private static IUser _owner { get; set; }

        internal static string DateString => $"{DateTime.Now:s}".Replace(":", ".");

        internal static int RandomNumber(int low, int high)
        {
            lock (Synclock)
            {
                return Random.Next(low, high);
            }
        }
    }
}