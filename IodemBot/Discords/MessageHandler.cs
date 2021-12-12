using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IodemBot.Core.Leveling;
using IodemBot.Modules;

namespace IodemBot
{
    public class MessageHandler
    {
        private DiscordSocketClient _client;
        private List<AutoResponse> _responses;

        public async Task InitializeAsync(DiscordSocketClient client)
        {
            this._client = client;
            client.MessageReceived += HandleMessageAsync;
            //badWords = File.ReadAllLines("Resources/bad_words.txt");

            _responses = new List<AutoResponse>
            {
                new(
                    new Regex("[Hh][y][a]+[h][o]+", RegexOptions.Compiled),
                    new Reaction("",
                        Emote.Parse("<:Keelhaul:537265959442841600>")),
                    5),
                new(
                    new Regex("[H][Y][A]+[H][O]+", RegexOptions.Compiled),
                    new Reaction("",
                        Emote.Parse("<:Vicious_Chop:537265959384121366>")),
                    5),
                new(
                    new Regex("(Bubebo)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                    new Reaction("Do you feel the earth rumbling? It must be Lord Babi rolling in his grave.",
                        Emote.Parse("<:sad:685656410352386127>")),
                    60),
                new(
                    new Regex("(Air).*(Rock).*(Sol Blade)|(Sol Blade).*(Air).*(Rock)",
                        RegexOptions.Compiled | RegexOptions.IgnoreCase),
                    new Reaction(
                        "I assume you are talking about the Air's Rock Glitch where you can get an early Sol Blade. Check TLPlexas video about it! https://www.youtube.com/watch?v=AIdt53_mqXQ&t=1s"),
                    30),
                new(
                    new Regex(@"\¡\!", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                    new Reaction(
                        "If you want to summon me to seek my assistance, use the prefix `i!` as in **I**odem."),
                    30),
                new(
                    new Regex(@"(\#\^\@\%\!)", RegexOptions.Compiled),
                    new CurseReaction(),
                    2),
                new(
                    new Regex(@"(^|\D)(420)(\D|$)", RegexOptions.Compiled),
                    new Reaction("",
                        Emote.Parse("<:Herb:543043292187590659>")),
                    60),
                new(
                    new Regex(@"Krakden", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                    new Reaction("",
                        Emote.Parse("<:Krakden:576856312500060161>")),
                    60)
            };
            await Task.CompletedTask;
        }

        private async Task HandleMessageAsync(SocketMessage s)
        {
            if (s is not SocketUserMessage msg) return;

            var context = new SocketCommandContext(_client, msg);
            if (context.User.IsBot) return;

            //Check for Profanity here

            // Auto Responses
            _responses.ForEach(r => _ = r.Check(msg));
            try
            {
                if (context.User is SocketGuildUser guildUser)
                    Leveling.UserSentMessage(guildUser, (SocketTextChannel)context.Channel);
            }
            catch (Exception e)
            {
                Console.WriteLine("UserSentMessage Error" + e);
                File.WriteAllText("Logs/MessageError_" + Global.DateString + ".txt", e.ToString());
            }

            await Task.CompletedTask;
        }

        private class CurseReaction : Reaction
        {
            public CurseReaction() : base("", Emote.Parse("<:curse:538074679492083742>"))
            {
            }

            public override async Task ReactAsync(SocketUserMessage msg)
            {
                _ = base.ReactAsync(msg);
                _ = GoldenSunCommands.AwardClassSeries("Curse Mage Series", msg.Author, msg.Channel);
                await Task.CompletedTask;
            }
        }

        internal class AutoResponse
        {
            public DateTime LastUse;
            public Reaction Reaction;
            public int TimeOut;
            public Regex Trigger;

            public AutoResponse(Regex regex, Reaction reaction, int timeOut)
            {
                Trigger = regex;
                this.Reaction = reaction;
                this.TimeOut = timeOut;
                LastUse = DateTime.MinValue;
            }

            public async Task Check(SocketUserMessage msg)
            {
                if (Trigger.IsMatch(msg.Content))
                {
                    if ((DateTime.Now - LastUse).TotalSeconds < TimeOut) return;

                    _ = Reaction.ReactAsync(msg);
                    LastUse = DateTime.Now;
                }

                await Task.CompletedTask;
            }
        }

        internal class Reaction
        {
            private readonly IEmote[] _emotes;
            private readonly string _text;

            public Reaction(string text)
            {
                this._text = text;
                _emotes = new IEmote[] { };
            }

            public Reaction(string text, IEmote[] emotes)
            {
                this._text = text;
                this._emotes = emotes;
            }

            public Reaction(string text, IEmote emote)
            {
                this._text = text;
                _emotes = new[] { emote };
            }

            public virtual async Task ReactAsync(SocketUserMessage msg)
            {
                if (_emotes.Length > 0) _ = msg.AddReactionsAsync(_emotes);

                if (_text != "")
                {
                    var embed = new EmbedBuilder();
                    embed.WithColor(Colors.Get("Iodem"));
                    embed.WithDescription(_text);
                    _ = msg.Channel.SendMessageAsync("", false, embed.Build());
                }

                await Task.CompletedTask;
            }
        }
    }
}