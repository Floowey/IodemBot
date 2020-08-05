﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.Modules.ColossoBattles
{
    public abstract class PvEEnvironment : BattleEnvironment
    {
        public abstract BattleDifficulty Difficulty { get; }
        protected IUserMessage EnemyMessage = null;
        protected IUserMessage StatusMessage = null;
        protected IUserMessage SummonsMessage = null;
        public ITextChannel BattleChannel = null;
        protected Dictionary<IUserMessage, PlayerFighter> PlayerMessages = new Dictionary<IUserMessage, PlayerFighter>();
        protected bool wasJustReset = true;
        private bool WasReset = false;
        public PlayerFighterFactory Factory { get; set; } = new PlayerFighterFactory();

        internal override ulong[] GetIds => new[] { BattleChannel.Id };

        public PvEEnvironment(string Name, ITextChannel lobbyChannel, bool isPersistent, ITextChannel BattleChannel) : base(Name, lobbyChannel, isPersistent)
        {
            this.BattleChannel = BattleChannel;
            this.lobbyChannel = lobbyChannel;
        }

        private async Task Initialize()
        {
            EnemyMessage = await BattleChannel.SendMessageAsync(GetEnemyMessageString());
            _ = EnemyMessage.AddReactionsAsync(new IEmote[]
                {
                    Emote.Parse("<:Fight:536919792813211648>"),
                    Emote.Parse("<:Battle:536954571256365096>")
                });
            SummonsMessage = await BattleChannel.SendMessageAsync("Good Luck!");
            return;
        }

        public override void Dispose()
        {
            base.Dispose();
            if (!IsPersistent)
            {
                _ = BattleChannel.DeleteAsync();
            }
        }

        protected virtual string GetEnemyMessageString()
        {
            return $"Welcome to {Name} Battle!\n\nReact with <:Fight:536919792813211648> to join the {Name} Battle and press <:Battle:536954571256365096> when you are ready to battle!";
        }

        protected virtual string GetStartBattleString()
        {
            string msg = PlayerMessages
                        .Aggregate("", (s, v) => s += $"<@{v.Value.avatar.ID}>, ");
            return $"{msg} get into Position!";
        }

        protected virtual string GetWinMessageString()
        {
            var winners = Battle.GetTeam(Battle.GetWinner());
            return $"{winners.FirstOrDefault().Name}'s Party wins! Battle will reset shortly.";
        }

        protected virtual string GetLossMessage()
        {
            return GetWinMessageString();
        }

        public abstract void SetEnemy(string Enemy);

        public abstract void SetNextEnemy();

        protected override async Task ProcessReaction(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            try
            {
                if (reaction.User.Value.IsBot)
                {
                    return;
                }
                else if (channel.Id != BattleChannel.Id)
                {
                    return;
                }
                else if (reaction.Emote.Name == "Fight")
                {
                    _ = AddPlayer(reaction);
                    return;
                }
                else if (reaction.Emote.Name == "Battle")
                {
                    //File.AppendAllText("Logs/BattleStats.txt", $"{DateTime.Now},{Name}, {GetIds}\n");
                    _ = StartBattle();
                    return;
                }

                if (new[] { "Bronze", "Silver", "Gold" }.Contains(reaction.Emote.Name) && this is SingleBattleEnvironment environment)
                {
                    Dictionary<string, BattleDifficulty> diff = new Dictionary<string, BattleDifficulty>()
                    {
                        { "Bronze", BattleDifficulty.Easy },
                        { "Silver", BattleDifficulty.Medium },
                        { "Gold", BattleDifficulty.Hard }
                    };
                    environment.internalDiff = diff[reaction.Emote.Name];
                    await Reset();
                    return;
                }

                IUserMessage c = null;
                if (StatusMessage.Id == reaction.MessageId)
                {
                    c = StatusMessage;
                }
                if (EnemyMessage.Id == reaction.MessageId)
                {
                    c = EnemyMessage;
                }
                if (SummonsMessage.Id == reaction.MessageId)
                {
                    c = SummonsMessage;
                }
                if (PlayerMessages.Keys.Any(k => k.Id == reaction.MessageId))
                {
                    c = PlayerMessages.Keys.Where(k => k.Id == reaction.MessageId).First();
                }

                if (c == null)
                {
                    c = (IUserMessage)await channel.GetMessageAsync(reaction.MessageId);
                    _ = c.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                    Console.WriteLine("No matching Message for User found.");
                    return;
                }

                if (!Battle.isActive)
                {
                    _ = c.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                    Console.WriteLine("Battle not active.");
                    return;
                }

                if (Battle.turnActive)
                {
                    _ = c.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                    Console.WriteLine("Not so fast");
                    return;
                }

                if (reaction.Emote.Name == "🔄")
                {
                    await c.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                    _ = RedrawBattle();
                    return;
                }

                if (reaction.Emote.Name == "⏸️")
                {
                    autoTurn.Stop();
                    return;
                }

                if (reaction.Emote.Name == "▶")
                {
                    autoTurn.Start();
                    return;
                }

                if (reaction.Emote.Name == "⏩")
                {
                    _ = ProcessTurn(true);
                    return;
                }

                var curPlayer = PlayerMessages.Values.Where(p => p.avatar.ID == reaction.User.Value.Id).FirstOrDefault();
                if (curPlayer == null)
                {
                    _ = c.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                    Console.WriteLine("Player not in this room.");
                    return;
                }
                var correctID = PlayerMessages.Keys.Where(key => PlayerMessages[key].avatar.ID == curPlayer.avatar.ID).First().Id;

                if (!numberEmotes.Contains(reaction.Emote.Name))
                {
                    if (reaction.MessageId != EnemyMessage.Id && reaction.MessageId != SummonsMessage.Id && reaction.MessageId != correctID)
                    {
                        _ = c.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        Console.WriteLine("Didn't click on own message.");
                        return;
                    }
                }

                if (!curPlayer.Select(reaction.Emote))
                {
                    _ = c.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                    Console.WriteLine("Couldn't select that move.");
                    return;
                }
                reactions.Add(reaction);

                _ = ProcessTurn(forced: false);
            }
            catch (Exception e)
            {
                Console.WriteLine("Colosso Turn Processing Error: " + e);
                File.WriteAllText($"Logs/Crashes/Error_{DateTime.Now.Date}.log", e.ToString());
            }
        }

        protected async Task RedrawBattle()
        {
            autoTurn.Stop();
            wasJustReset = false;
            Task.WaitAll(PlayerMessages.Select(m => m.Key.RemoveAllReactionsAsync()).Append(EnemyMessage.RemoveAllReactionsAsync()).ToArray());

            await WriteBattleInit();
            autoTurn.Start();
        }

        protected virtual async Task AddPlayer(SocketReaction reaction)
        {
            if (PlayerMessages.Values.Any(s => (s.avatar.ID == reaction.UserId)))
            {
                return;
            }
            SocketGuildUser player = (SocketGuildUser)reaction.User.Value;
            var playerAvatar = UserAccounts.GetAccount(player);

            var p = Factory.CreatePlayerFighter(player);
            await AddPlayer(p);
        }

        protected virtual async Task AddPlayer(PlayerFighter player)
        {
            if (Battle.isActive)
            {
                return;
            }

            Battle.AddPlayer(player, Team.A);

            var playerMsg = await BattleChannel.SendMessageAsync($"{player.Name} wants to battle!");
            PlayerMessages.Add(playerMsg, player);
            resetIfNotActive.Stop();
            resetIfNotActive.Start();

            if (PlayerMessages.Count == PlayersToStart)
            {
                await StartBattle();
            }
        }

        public override async Task Reset()
        {
            Battle = new ColossoBattle();
            if (!IsPersistent && WasReset)
            {
                Dispose(); return;
            }
            WasReset = true;
            foreach (var k in PlayerMessages.Keys)
            {
                PlayerMessages[k].Moves.OfType<Djinn>().ToList().ForEach(d =>
                {
                    d.CoolDown = 0; d.Summon(PlayerMessages[k]);
                });
                await k.DeleteAsync();
            }
            Factory.uniqueDjinn.Clear();
            Factory.summons.Clear();
            PlayerMessages.Clear();

            if (EnemyMessage == null)
            {
                await Initialize();
            }
            else
            {
                await EnemyMessage.ModifyAsync(c => { c.Content = GetEnemyMessageString(); c.Embed = null; });
                await EnemyMessage.RemoveAllReactionsAsync();
                _ = EnemyMessage.AddReactionsAsync(new IEmote[]
                {
                        Emote.Parse("<:Fight:536919792813211648>"),
                        Emote.Parse("<:Battle:536954571256365096>")
                });
                wasJustReset = true;
            }

            if (SummonsMessage == null)
            {
                await Initialize();
            }
            else
            {
                _ = SummonsMessage.ModifyAsync(m => { m.Content = "Good Luck!"; m.Embed = null; });
                _ = SummonsMessage.RemoveAllReactionsAsync();
            }

            if (StatusMessage != null)
            {
                _ = StatusMessage.DeleteAsync();
                StatusMessage = null;
            }
            SetNextEnemy();

            if (autoTurn != null)
            {
                autoTurn.Dispose();
            }
            if (resetIfNotActive != null)
            {
                resetIfNotActive.Dispose();
            }
            autoTurn = new Timer()
            {
                Interval = 45000,
                AutoReset = false,
                Enabled = false
            };
            autoTurn.Elapsed += TurnTimeElapsed;
            resetIfNotActive = new Timer()
            {
                Interval = 120000,
                AutoReset = false,
                Enabled = false
            };
            resetIfNotActive.Elapsed += BattleWasNotStartetInTime;

            Console.WriteLine("Battle was reset.");
        }

        private async void BattleWasNotStartetInTime(object sender, ElapsedEventArgs e)
        {
            _ = Reset();
            await Task.CompletedTask;
        }

        private async void TurnTimeElapsed(object sender, ElapsedEventArgs e)
        {
            _ = ProcessTurn(forced: true);
            await Task.CompletedTask;
        }

        protected override async Task StartBattle()
        {
            if (Battle.isActive)
            {
                return;
            }

            if (Battle.SizeTeamA == 0)
            {
                return;
            }

            PlayerMessages.Values.ToList().ForEach(p => p.Moves.AddRange(Factory.PossibleSummons));

            resetIfNotActive.Stop();
            Battle.Start();
            await WriteBattleInit();
            autoTurn.Start();
            wasJustReset = false;
        }

        protected override async Task WriteBattle()
        {
            try
            {
                await WriteStatus();
                await WriteSummons();
                await WriteEnemies();
                await WritePlayers();
            }
            catch (HttpException e)
            {
                Console.WriteLine("Failed drawing Battle, retrying." + e.ToString());
                Battle.log.Add("Failed drawing Battle, retrying.");
                await WriteStatus();
                await WriteSummons();
                await WriteEnemies();
                await WritePlayers();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception while writing Battle" + e.ToString());
                throw new Exception("Exception while writing Battle", e);
            }
        }

        protected override async Task WriteBattleInit()
        {
            try
            {
                await WriteStatusInit();
                await WriteSummonsInit();
                await WriteEnemiesInit();
                await WritePlayersInit();
            }
            catch (HttpException e)
            {
                Console.WriteLine("Failed drawing Battle, retrying:" + e.ToString());
                Battle.log.Add("Failed drawing Battle, retrying.");
                await WriteStatusInit();
                await WriteSummonsInit();
                await WriteEnemiesInit();
                await WritePlayersInit();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception while writing Battle:" + e.ToString());
                throw new Exception("Exception while writing Battle", e);
            }
        }

        protected virtual async Task WriteEnemies()
        {
            var tasks = new List<Task>()
            {
                WriteEnemyEmbed()
            };

            var validReactions = reactions.Where(r => r.MessageId == EnemyMessage.Id).ToList();
            foreach (var r in validReactions)
            {
                tasks.Add(EnemyMessage.RemoveReactionAsync(r.Emote, r.User.Value));
                reactions.Remove(r);
            }
            await Task.WhenAll(tasks);
        }

        protected virtual async Task WriteEnemiesInit()
        {
            var tasks = new List<Task>
            {
                WriteEnemyEmbed(),
                WriteEnemyReactions()
            };
            await Task.WhenAll(tasks);
        }

        protected virtual async Task WriteEnemyEmbed()
        {
            var tasks = new List<Task>();
            var e = GetEnemyEmbedBuilder();
            if (EnemyMessage.Embeds.Count == 0 || !EnemyMessage.Embeds.FirstOrDefault().ToEmbedBuilder().AllFieldsEqual(e))
            {
                tasks.Add(EnemyMessage.ModifyAsync(m =>
                {
                    m.Content = ""; m.Embed = e.Build();
                }));
            }
            await Task.WhenAll(tasks);
        }

        protected virtual EmbedBuilder GetEnemyEmbedBuilder()
        {
            var e = new EmbedBuilder();
            if (Battle.SizeTeamB > 0)
            {
                e.WithThumbnailUrl(Battle.GetTeam(Team.B).FirstOrDefault().ImgUrl);
            }
            var i = 1;
            foreach (ColossoFighter fighter in Battle.GetTeam(Team.B))
            {
                e.AddField($"{numberEmotes[i]} {fighter.ConditionsToString()}", $"{fighter.Name}", true);
                i++;
            }
            return e;
        }

        protected virtual async Task WriteEnemyReactions()
        {
            var msg = EnemyMessage;
            var tasks = new List<Task>();
            var oldReactionCount = EnemyMessage.Reactions.Where(k => numberEmotes.Contains(k.Key.Name)).Count();
            if (wasJustReset)
            {
                await msg.RemoveAllReactionsAsync();
                Console.WriteLine("Reactions Cleared");
                oldReactionCount = 0;
            }

            if (Battle.SizeTeamB == oldReactionCount)
            {
            }
            else if (Battle.SizeTeamB <= 1)
            {
                if (oldReactionCount > 0)
                {
                    tasks.Add(msg.RemoveAllReactionsAsync());
                }
            }
            else if (Battle.SizeTeamB > oldReactionCount)
            {
                tasks.Add(msg.AddReactionsAsync(
                    numberEmotes
                    .Skip(Math.Max(1, oldReactionCount))
                    .Take(Battle.SizeTeamB - Math.Max(0, oldReactionCount - 1))
                    .Select(s => new Emoji(s))
                    .ToArray()));
            }
            else if (oldReactionCount - Battle.SizeTeamB <= Battle.SizeTeamB + 1)
            {
                var reactionsToRemove = msg.Reactions.Where(k => numberEmotes.Skip(Battle.SizeTeamB + 1).Contains(k.Key.Name)).ToArray();
                tasks.Add(msg.RemoveReactionsAsync(msg.Author, reactionsToRemove.Select(d => d.Key).ToArray()));
            }
            else
            {
                if (oldReactionCount > 0)
                {
                    await msg.RemoveAllReactionsAsync();
                }

                tasks.Add(msg.AddReactionsAsync(
                   numberEmotes
                   .Skip(1)
                   .Take(Battle.SizeTeamB)
                   .Select(s => new Emoji(s))
                   .ToArray()));
            }

            await Task.WhenAll(tasks);
        }

        protected virtual async Task WriteSummonsInit()
        {
            _ = WriteSummonsReactions();
            await WriteSummons();
        }

        protected virtual EmbedBuilder GetDjinnEmbedBuilder()
        {
            var allDjinn = PlayerMessages.Values.SelectMany(p => p.Moves.OfType<Djinn>()).ToList();
            var standbyDjinn = allDjinn.Where(d => d.State == DjinnState.Standby);
            var recoveryDjinn = allDjinn.Where(d => d.State == DjinnState.Recovery);
            if (allDjinn.Count == 0)
            {
                return null;
            }
            EmbedBuilder embed = new EmbedBuilder()
                .WithThumbnailUrl("https://cdn.discordapp.com/attachments/497696510688100352/640300243820216336/unknown.png");

            if (allDjinn.OfElement(Element.Venus).Count() > 0)
            {
                embed.AddField("Venus", $"{string.Join(" ", standbyDjinn.OfElement(Element.Venus).Select(d => d.Emote))} |" +
                    $"{string.Join(" ", recoveryDjinn.OfElement(Element.Venus).Select(d => d.Emote))}", true);
            }
            if (allDjinn.OfElement(Element.Mars).Count() > 0)
            {
                embed.AddField("Mars", $"{string.Join(" ", standbyDjinn.OfElement(Element.Mars).Select(d => d.Emote))} |" +
                    $"{string.Join(" ", recoveryDjinn.OfElement(Element.Mars).Select(d => d.Emote))}", true);
            }
            if (allDjinn.OfElement(Element.Jupiter).Count() > 0)
            {
                embed.AddField("Jupiter", $"{string.Join(" ", standbyDjinn.OfElement(Element.Jupiter).Select(d => d.Emote))} |" + $"{string.Join(" ", recoveryDjinn.OfElement(Element.Jupiter).Select(d => d.Emote))}", true);
            }
            if (allDjinn.OfElement(Element.Mercury).Count() > 0)
            {
                embed.AddField("Mercury", $"{string.Join(" ", standbyDjinn.OfElement(Element.Mercury).Select(d => d.Emote))} |" + $"{string.Join(" ", recoveryDjinn.OfElement(Element.Mercury).Select(d => d.Emote))}", true);
            }
            return embed;
        }

        protected virtual async Task WriteSummonsReactions()
        {
            _ = SummonsMessage.AddReactionsAsync(Factory.PossibleSummons.Select(s => s.GetEmote()).ToArray());
            await Task.CompletedTask;
        }

        protected virtual async Task WriteSummons()
        {
            var tasks = new List<Task>();
            var embed = GetDjinnEmbedBuilder();
            if (embed != null && (SummonsMessage.Embeds.Count == 0 || !SummonsMessage.Embeds.FirstOrDefault().ToEmbedBuilder().AllFieldsEqual(embed)))
            {
                _ = SummonsMessage.ModifyAsync(m => m.Embed = embed.Build());
            }

            var validReactions = reactions.Where(r => r.MessageId == SummonsMessage.Id).ToList();
            foreach (var r in validReactions)
            {
                tasks.Add(SummonsMessage.RemoveReactionAsync(r.Emote, r.User.Value));
                reactions.Remove(r);
            }
            await Task.CompletedTask;
        }

        protected virtual async Task WritePlayers()
        {
            int i = 1;
            var tasks = new List<Task>();
            foreach (KeyValuePair<IUserMessage, PlayerFighter> k in PlayerMessages)
            {
                var msg = k.Key;
                var embed = new EmbedBuilder();
                var fighter = k.Value;

                var validReactions = reactions.Where(r => r.MessageId == msg.Id).ToList();
                foreach (var r in validReactions)
                {
                    tasks.Add(msg.RemoveReactionAsync(r.Emote, r.User.Value));
                    reactions.Remove(r);
                }
                embed.WithThumbnailUrl(fighter.ImgUrl);
                embed.WithColor(Colors.Get(fighter.Moves.OfType<Psynergy>().Select(p => p.Element.ToString()).ToArray()));
                embed.AddField($"{numberEmotes[i]}{fighter.ConditionsToString()}", fighter.Name, true);
                embed.AddField("HP", $"{fighter.Stats.HP} / {fighter.Stats.MaxHP}", true);
                embed.AddField("PP", $"{fighter.Stats.PP} / {fighter.Stats.MaxPP}", true);
                var s = new List<string>();
                foreach (var m in fighter.Moves)
                {
                    if (m is Psynergy p)
                    {
                        s.Add($"{m.Emote} {m.Name} {p.PPCost}");
                    }
                    else if (m is Summon summon)
                    {
                    }
                    else
                    {
                        s.Add($"{m.Emote} {m.Name}");
                    }
                }
                embed.AddField("Psynergy", string.Join(" | ", s));

                if (msg.Embeds.Count == 0 || !msg.Embeds.FirstOrDefault().ToEmbedBuilder().AllFieldsEqual(embed))
                {
                    tasks.Add(msg.ModifyAsync(m => { m.Content = $""; m.Embed = embed.Build(); }));
                }

                if (fighter is PlayerFighter && (fighter).AutoTurnsInARow >= 2)
                {
                    var ping = await msg.Channel.SendMessageAsync($"<@{(fighter).avatar.ID}>");
                    await ping.DeleteAsync();
                }
                i++;
            }
            await Task.WhenAll(tasks);
        }

        protected virtual async Task WritePlayersInit()
        {
            int i = 1;
            var tasks = new List<Task>();
            foreach (KeyValuePair<IUserMessage, PlayerFighter> k in PlayerMessages)
            {
                var msg = k.Key;
                var fighter = k.Value;
                List<IEmote> emotes = new List<IEmote>();
                if (PlayerMessages.Count > 1)
                {
                    emotes.Add(new Emoji(numberEmotes[i]));
                }
                foreach (var m in fighter.Moves)
                {
                    if (!(m is Summon))
                    {
                        emotes.Add(m.GetEmote());
                    }
                }
                emotes.RemoveAll(e => msg.Reactions.Any(r => r.Key.Name.Equals(e.Name, StringComparison.InvariantCultureIgnoreCase)));
                tasks.Add(msg.AddReactionsAsync(emotes.ToArray()));
                i++;
            }
            tasks.Add(WritePlayers());
            await Task.WhenAll(tasks);
        }

        protected virtual async Task WriteStatusInit()
        {
            await WriteStatus();
        }

        protected virtual async Task WriteStatus()
        {
            if (Battle.log.Count > 0 && Battle.turn > 0)
            {
                if (StatusMessage == null)
                {
                    StatusMessage = await BattleChannel.SendMessageAsync(Battle.log.Aggregate("", (s, l) => s += l + "\n"));
                }
                else
                {
                    await StatusMessage.ModifyAsync(c => c.Content = Battle.log.Aggregate("", (s, l) => s += l + "\n"));
                }
            }
            else
            {
                if (StatusMessage == null)
                {
                    StatusMessage = await BattleChannel.SendMessageAsync(GetStartBattleString());
                }
            }
        }

        protected virtual async Task WriteGameOver()
        {
            await Task.Delay(3000);
            var text = GetWinMessageString();
            await StatusMessage.ModifyAsync(m => { m.Content = text; m.Embed = null; });
            await Task.Delay(2000);
            await Reset();
        }
    }
}