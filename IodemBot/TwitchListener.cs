using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Streams;

namespace IodemBot
{
    public class TwitchListener
    {
        private static TwitchAPI api;

        private static readonly Dictionary<string, string> GoldenSunIds = new Dictionary<string, string>() {
            { "252", "Golden Sun" },
            { "3916" , "Golden Sun: The Lost Age"},
            { "24232" ,"Golden Sun: Dark Dawn" },
            //{ "32399","Counter-Strike: Global Offensive" },
            //{"33214", "Fortnite" }
            //{"490744", "Stardew Valley" }
            //{"488552", "Overwatch" }
        };

        private static Timer timer;
        private static List<string> runningIds = new List<string>();
        private static IMessageChannel channel;

        public static Task InitializeAsync()
        {
            api = new TwitchAPI();
            api.Settings.ClientId = TwitchConfig.bot.clientID;
            api.Settings.AccessToken = TwitchConfig.bot.clientSecret;
            timer = new Timer()
            {
                Interval = 15 * 60 * 1000,
                AutoReset = true
            };
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
            channel = ((ISocketMessageChannel)Global.Client.GetChannel(511702094672298044)) ?? ((ISocketMessageChannel)Global.Client.GetChannel(497696510688100352));
            return Task.CompletedTask;
        }

        private static async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var streams = (await api.Helix.Streams.GetStreamsAsync(gameIds: GoldenSunIds.Keys.ToList())).Streams.ToList();

            var newStreams = streams
                .Where(s => !runningIds.Contains(s.Id)
                        && s.StartedAt > Global.RunningSince.ToUniversalTime().Subtract(new TimeSpan(0, 15, 0)))
                .ToList();
            //var newStreams = streams;
            runningIds = streams.Select(d => d.Id).ToList();
            Console.WriteLine($"Total Streams: {streams.Count()}, New Streams: {newStreams.Count()}");
            foreach (Stream s in newStreams)
            {
                var user = (await api.Helix.Users.GetUsersAsync(new List<string>() { s.UserId })).Users.FirstOrDefault();
                await channel
                    .SendMessageAsync("", false, new EmbedBuilder()
                    .WithAuthor($"{user.DisplayName} is streaming { GoldenSunIds[s.GameId]}", user.ProfileImageUrl, $"https://twitch.tv/{user.Login}")
                    .WithColor(100, 65, 100)
                    .WithDescription($"{s.Title} \n\n Now live on: \n https://twitch.tv/{user.Login}")
                    .WithThumbnailUrl(s.ThumbnailUrl.Replace("{width}", "300").Replace("{height}", "300"))
                    .WithFooter(user.Description)
                    .Build());
            }
        }

        public static async Task AllStreams(object sender, ElapsedEventArgs e)
        {
            var streams = (await api.Helix.Streams.GetStreamsAsync(gameIds: GoldenSunIds.Keys.ToList())).Streams.ToList();

            var newStreams = streams;
            //var newStreams = streams;
            runningIds = streams.Select(d => d.Id).ToList();
            Console.WriteLine($"Total Streams: {streams.Count()}, New Streams: {newStreams.Count()}");
            foreach (Stream s in newStreams)
            {
                var user = (await api.Helix.Users.GetUsersAsync(new List<string>() { s.UserId })).Users.FirstOrDefault();
                await channel
                    .SendMessageAsync("", false, new EmbedBuilder()
                    .WithAuthor($"{user.DisplayName} is streaming { GoldenSunIds[s.GameId]}", user.ProfileImageUrl, $"https://twitch.tv/{user.Login}")
                    .WithColor(100, 65, 100)
                    .WithDescription($"{s.Title} \n\n Now live on: \n https://twitch.tv/{user.Login}")
                    .WithThumbnailUrl(s.ThumbnailUrl.Replace("{width}", "300").Replace("{height}", "300"))
                    .WithFooter(user.Description)
                    .Build());
            }
        }

        public static async Task GetStreamers()
        {
            var games = await api.Helix.Games.GetGamesAsync(gameNames: new List<string>() { "Overwatch", "Stardew Valley", "Fortnite" });
            Timer_Elapsed(null, null);
        }
    }
}