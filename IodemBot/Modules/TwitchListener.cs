using System;
using System.Collections.Generic;
using System.Timers;
using TwitchLib.Api;

namespace IodemBot.Modules
{
    public class TwitchListener
    {
        private static TwitchAPI api = new TwitchAPI();
        private static readonly string client_id;
        private static readonly string token;
        private static readonly List<string> GoldenSunIds = new List<string>() { "", "" };

        static TwitchListener()
        {
            api.Settings.ClientId = client_id;
            api.Settings.AccessToken = "access_token";
            api.Helix.Streams.GetStreamsAsync(communityIds: new List<string>() { });
            Console.WriteLine("Timer Running");
        }

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
        }
    }
}