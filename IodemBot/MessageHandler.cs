using Discord;
using Discord.Commands;
using Discord.WebSocket;
using IodemBot.Core.Leveling;
using IodemBot.Core.UserManagement;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IodemBot
{
    public class MessageHandler
    {
        private DiscordSocketClient client;
        private CommandService service;
        private readonly ulong[] whiteList = { 1234 };
        private List<AutoResponse> responses;

        public async Task InitializeAsync(DiscordSocketClient client)
        {
            this.client = client;
            service = new CommandService();
            await service.AddModulesAsync(Assembly.GetEntryAssembly(), null);
            client.MessageReceived += HandleMessageAsync;
            client.ReactionAdded += HandleReactionAsync;
            //badWords = File.ReadAllLines("Resources/bad_words.txt");

            responses = new List<AutoResponse>
            {
                new AutoResponse(
                new Regex("[Hh][y][a]+[h][o]+", RegexOptions.Compiled),
                new Reaction("",
                    Emote.Parse("<:Keelhaul:537265959442841600>")),
                5),
                new AutoResponse(
                new Regex("[H][Y][A]+[H][O]+", RegexOptions.Compiled),
                new Reaction("",
                    Emote.Parse("<:Vicious_Chop:537265959384121366>")),
                5),
                new AutoResponse(
                new Regex("Bubebo", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                new Reaction("Do you feel the earth rumbling? It must be Lord Babi rolling in his grave.",
                    Emote.Parse("<:sad:490015818063675392>")),
                60),
                new AutoResponse(
                new Regex("(Air).*(Rock).*(Sol Blade)|(Sol Blade).*(Air).*(Rock)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                new Reaction("I assume you are talking about the Air's Rock Glitch where you can get an early Sol Blade. Check TLPlexas video about it! https://www.youtube.com/watch?v=AIdt53_mqXQ&t=1s"),
                30),
                new AutoResponse(
                new Regex(@"\¡\!", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                new Reaction("If you want to summon me to seek my assistance, use the prefix `i!` as in **I**odem."),
                30),
                new AutoResponse(
                new Regex(@"(\#\^\@\%\!)", RegexOptions.Compiled),
                new CurseReaction(),
                2),
                new AutoResponse(
                new Regex(@"(^|\D)(420)(\D|$)", RegexOptions.Compiled),
                new Reaction("",
                    Emote.Parse("<:Herb:543043292187590659>")),
                60),
                new AutoResponse(
                new Regex(@"Krakden", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                new Reaction("",
                    Emote.Parse("<:Krakden:576856312500060161>")),
                60)
            };
        }

        private async Task HandleReactionAsync(Cacheable<IUserMessage, ulong> Message, ISocketMessageChannel Channel, SocketReaction Reaction)
        {
            var User = (SocketGuildUser)Reaction.User;
            if (User.IsBot)
            {
                return;
            }
            Leveling.UserAddedReaction(User, Reaction);
            await Task.CompletedTask;
        }

        private async Task HandleMessageAsync(SocketMessage s)
        {
            if (!(s is SocketUserMessage msg))
            {
                return;
            }

            var context = new SocketCommandContext(client, msg);
            if (context.User.IsBot)
            {
                return;
            }

            //Check for Profanity here

            // Auto Responses
            responses.ForEach(async r => await r.Check(msg));
            Leveling.UserSentMessage((SocketGuildUser)context.User, (SocketTextChannel)context.Channel);
            await Task.CompletedTask;
        }

        private class CurseReaction : Reaction
        {
            public CurseReaction() : base("", Emote.Parse("<:curse:538074679492083742>"))
            {
            }

            public override async Task ReactAsync(SocketUserMessage msg)
            {
                await base.ReactAsync(msg);
                var userAccount = UserAccounts.GetAccount(msg.Author);
                userAccount.ServerStats.HasWrittenCurse = true;
                UserAccounts.SaveAccounts();
                await ServerGames.UserHasCursed((SocketGuildUser)msg.Author, (SocketTextChannel)msg.Channel);
            }
        }

        private bool ContainsBadWord(SocketUserMessage msg)
        {
            //you should do this once and not every function call
            return false;
        }

        internal async Task CheckProfanity(SocketUserMessage msg)
        {
            await Task.CompletedTask;
        }

        internal class AutoResponse
        {
            public Regex trigger;
            public Reaction reaction;
            public DateTime lastUse;
            public int timeOut;

            public AutoResponse(Regex regex, Reaction reaction, int timeOut)
            {
                trigger = regex;
                this.reaction = reaction;
                this.timeOut = timeOut;
                lastUse = DateTime.MinValue;
            }

            public async Task Check(SocketUserMessage msg)
            {
                if (trigger.IsMatch(msg.Content))
                {
                    if ((DateTime.Now - lastUse).TotalSeconds < timeOut)
                    {
                        return;
                    }

                    await reaction.ReactAsync(msg);
                    lastUse = DateTime.Now;
                }
            }
        }

        internal class Reaction
        {
            private readonly string text;
            private readonly IEmote[] emotes;

            public Reaction(string text)
            {
                this.text = text;
                emotes = new IEmote[] { };
            }

            public Reaction(string text, IEmote[] emotes)
            {
                this.text = text;
                this.emotes = emotes;
            }

            public Reaction(string text, IEmote emote)
            {
                this.text = text;
                this.emotes = new IEmote[] { emote };
            }

            public virtual async Task ReactAsync(SocketUserMessage msg)
            {
                if (emotes.Length > 0)
                {
                    await msg.AddReactionsAsync(emotes);
                }

                if (text != "")
                {
                    var embed = new EmbedBuilder();
                    embed.WithColor(Colors.Get("Iodem"));
                    embed.WithDescription(text);
                    await msg.Channel.SendMessageAsync("", false, embed.Build());
                }
            }
        }
    }
}