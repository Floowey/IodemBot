﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;
using IodemBot.Core.Leveling;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.Modules.ColossoBattles
{
    public abstract class PvPEnvironment : BattleEnvironment
    {
        protected class PvPTeamCollector
        {
            public Team team;
            public Team enemies;
            public ITextChannel teamChannel;
            public IUserMessage EnemyMessage = null;
            public IUserMessage StatusMessage = null;
            public IUserMessage SummonsMessage = null;
            public PlayerFighterFactory Factory = new PlayerFighterFactory() { LevelOption = LevelOption.SetLevel, SetLevel = 60 };
            public Dictionary<IUserMessage, PlayerFighter> PlayerMessages = new Dictionary<IUserMessage, PlayerFighter>();
        }

        private readonly uint PlayersToStartB = 4;
        private readonly List<SocketGuildUser> playersWithBRole = new List<SocketGuildUser>();
        public IRole TeamBRole;

        internal override ulong[] GetIds => new[] { Teams[Team.A].teamChannel.Id, Teams[Team.B].teamChannel.Id };

        protected Dictionary<Team, PvPTeamCollector> Teams = new Dictionary<Team, PvPTeamCollector>()
        {
            {Team.A, new PvPTeamCollector(){team = Team.A, enemies = Team.B } },
            {Team.B, new PvPTeamCollector(){team = Team.B, enemies = Team.A } },
        };

        public PvPEnvironment(string Name, ITextChannel lobbyChannel, bool isPersistent, ITextChannel teamAChannel, ITextChannel teamBChannel, IRole TeamBRole, uint playersToStart = 3, uint playersToStartB = 3) : base(Name, lobbyChannel, isPersistent)
        {
            PlayersToStart = playersToStart;
            PlayersToStartB = playersToStartB;
            this.TeamBRole = TeamBRole;
            Teams[Team.A].teamChannel = teamAChannel;
            Teams[Team.B].teamChannel = teamBChannel;
            Initialize();
        }

        private async void Initialize()
        {
            var A = Teams[Team.A];
            var B = Teams[Team.B];

            A.EnemyMessage = await A.teamChannel.SendMessageAsync($"Welcome to {Name}. Join Team A or Team B.");
            _ = A.EnemyMessage.AddReactionsAsync(new IEmote[]
                {
                    Emote.Parse("<:Fight_A:592374736479059979>"),
                    Emote.Parse("<:Fight_B:592374736248373279>"),
                    Emote.Parse("<:Battle:536954571256365096>")
                });
            A.SummonsMessage = await A.teamChannel.SendMessageAsync("Good Luck!");
            B.EnemyMessage = await B.teamChannel.SendMessageAsync($"Welcome to {Name}, Team B. Please wait til the battle has started.");
            B.SummonsMessage = await B.teamChannel.SendMessageAsync("Good Luck!");
            return;
        }

        public override async Task Reset()
        {
            Battle = new ColossoBattle();
            var A = Teams[Team.A];
            var B = Teams[Team.B];

            playersWithBRole.Where(p => p.Roles.Any(r => r.Name == "TeamB")).ToList().ForEach(a => _ = a.RemoveRoleAsync(TeamBRole));

            foreach (var k in A.PlayerMessages.Keys)
            {
                await k.DeleteAsync();
            }
            foreach (var k in B.PlayerMessages.Keys)
            {
                await k.DeleteAsync();
            }

            A.PlayerMessages.Clear();
            B.PlayerMessages.Clear();

            if (A.EnemyMessage != null)
            {
                await A.EnemyMessage.RemoveAllReactionsAsync();
                _ = A.EnemyMessage.ModifyAsync(m =>
                {
                    m.Content = $"Welcome to {Name}. Join Team A or Team B.";
                    m.Embed = null;
                });
                _ = A.EnemyMessage.AddReactionsAsync(new IEmote[]
                {
                    Emote.Parse("<:Fight_A:592374736479059979>"),
                    Emote.Parse("<:Fight_B:592374736248373279>"),
                    Emote.Parse("<:Battle:536954571256365096>")
                });
            }
            if (B.EnemyMessage != null)
            {
                _ = B.EnemyMessage.RemoveAllReactionsAsync();
                _ = B.EnemyMessage.ModifyAsync(m =>
                {
                    m.Content = $"Welcome to {Name}, Team B. Please wait til the battle has started.";
                    m.Embed = null;
                });
            }
            if (A.StatusMessage != null)
            {
                _ = A.StatusMessage.DeleteAsync();
                A.StatusMessage = null;
            }

            if (B.StatusMessage != null)
            {
                _ = B.StatusMessage.DeleteAsync();
                B.StatusMessage = null;
            }

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
                Interval = 60000,
                AutoReset = false,
                Enabled = false
            };
            autoTurn.Elapsed += TurnTimeElapsed;
            resetIfNotActive = new Timer()
            {
                Interval = 240000,
                AutoReset = false,
                Enabled = false
            };
            resetIfNotActive.Elapsed += BattleWasNotStartedInTime;

            Console.WriteLine("Battle was reset.");
        }

        private async void BattleWasNotStartedInTime(object sender, ElapsedEventArgs e)
        {
            await Reset();
        }

        private async void TurnTimeElapsed(object sender, ElapsedEventArgs e)
        {
            _ = ProcessTurn(forced: true);
            await Task.CompletedTask;
        }

        protected override async Task GameOver()
        {
            var winners = Battle.GetTeam(Battle.GetWinner());
            var losers = winners.First().battle.GetTeam(winners.First().enemies);

            winners.ConvertAll(s => (PlayerFighter)s).ForEach(async p => await ServerGames.UserWonPvP(p.avatar, lobbyChannel, winners.Count, losers.Count));

            _ = WriteGameOver();
            await Task.CompletedTask;
        }

        private async Task WriteGameOver()
        {
            await Task.Delay(5000);
            var winners = Battle.GetTeam(Battle.GetWinner());
            var text = $"{winners.FirstOrDefault().Name}'s Party wins! Battle will reset shortly.";

            _ = Teams[Team.A].StatusMessage.ModifyAsync(m => { m.Content = text; m.Embed = null; });
            _ = Teams[Team.B].StatusMessage.ModifyAsync(m => { m.Content = text; m.Embed = null; });

            await Task.Delay(5000);
            _ = Reset();
        }

        protected override async Task ProcessReaction(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            try
            {
                if (reaction.User.Value.IsBot)
                {
                    return;
                }
                if (channel.Id != Teams[Team.A].teamChannel.Id && channel.Id != Teams[Team.B].teamChannel.Id)
                {
                    return;
                }
                if (reaction.Emote.Name == "Fight_A")
                {
                    _ = AddPlayer(reaction, Team.A);
                    return;
                }
                if (reaction.Emote.Name == "Fight_B")
                {
                    _ = AddPlayer(reaction, Team.B);
                    return;
                }
                else if (reaction.Emote.Name == "Battle")
                {
                    _ = StartBattle();
                    return;
                }

                Teams.Values.ToList().ForEach(async V =>
                {
                    var StatusMessage = V.StatusMessage;
                    var PlayerMessages = V.PlayerMessages;
                    var EnemyMessage = V.EnemyMessage;
                    var SummonsMessage = V.SummonsMessage;
                    if (channel.Id != V.teamChannel.Id)
                    {
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
                        autoTurn.Stop();
                        Task.WaitAll(PlayerMessages.Select(m => m.Key.RemoveAllReactionsAsync()).Append(EnemyMessage.RemoveAllReactionsAsync()).ToArray());

                        _ = WriteBattleInit();
                        autoTurn.Start();
                        return;
                    }

                    if (reaction.Emote.Name == "⏸")
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
                        if (reaction.MessageId != EnemyMessage.Id && reaction.MessageId != correctID)
                        {
                        }
                    }

                    if (!curPlayer.Select(reaction.Emote))
                    {
                        _ = c.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        Console.WriteLine("Couldn't select that move.");
                        return;
                    }
                    reactions.Add(reaction);
                });

                _ = ProcessTurn(forced: false);
            }
            catch (Exception e)
            {
                Console.WriteLine("Colosso Turn Processing Error: " + e.Message);
                File.WriteAllText($"Logs/Crashes/Error_{DateTime.Now.Date}.log", e.Message);
            }
            await Task.CompletedTask;
        }

        protected virtual async Task AddPlayer(SocketReaction reaction, Team team)
        {
            if (Teams[Team.A].PlayerMessages.Values.Any(s => (s.avatar.ID == reaction.UserId)))
            {
                return;
            }
            if (Teams[Team.B].PlayerMessages.Values.Any(s => (s.avatar.ID == reaction.UserId)))
            {
                return;
            }
            SocketGuildUser player = (SocketGuildUser)reaction.User.Value;
            if (team == Team.B)
            {
                await player.AddRoleAsync(TeamBRole);
                playersWithBRole.Add(player);
            }
            var playerAvatar = UserAccounts.GetAccount(player);

            var factory = Teams[team].Factory;
            var p = factory.CreatePlayerFighter(player);
            await AddPlayer(p, team);
        }

        protected virtual async Task AddPlayer(PlayerFighter player, Team team)
        {
            if (Battle.isActive)
            {
                return;
            }

            Battle.AddPlayer(player, team);

            var playerMsg = await Teams[team].teamChannel.SendMessageAsync($"{player.Name} wants to battle!");
            Teams[team].PlayerMessages.Add(playerMsg, player);
            resetIfNotActive.Start();

            if (Teams[Team.A].PlayerMessages.Count == PlayersToStart && Teams[Team.B].PlayerMessages.Count == PlayersToStartB)
            {
                await StartBattle();
            }
        }

        protected override async Task StartBattle()
        {
            if (Battle.isActive)
            {
                return;
            }

            if (Battle.SizeTeamA == 0 || Battle.SizeTeamB == 0)
            {
                return;
            }


            foreach (var V in Teams.Values)
            {
                V.PlayerMessages.Values.ToList().ForEach(p => p.Moves.AddRange(V.Factory.PossibleSummons));
            }


            resetIfNotActive.Stop();
            Battle.Start();
            await WriteBattleInit();
            autoTurn.Start();
        }

        protected override async Task WriteBattle()
        {
            await Task.WhenAll(new[]
            {
                WriteStatus(),
                WriteEnemies(),
                WriteSummons(),
                WritePlayers()
            }); ;
        }

        protected override async Task WriteBattleInit()
        {
            await WriteStatusInit();
            await WriteEnemiesInit();
            await WriteSummonsInit();
            await WritePlayersInit();
        }

        protected virtual async Task WriteEnemies()
        {
            var tasks = new List<Task>()
            {
                WriteEnemyEmbeds()
            };

            Teams.Values.ToList().ForEach(V =>
            {
                var validReactions = reactions.Where(r => r.MessageId == V.EnemyMessage.Id).ToList();
                foreach (var r in validReactions)
                {
                    tasks.Add(V.EnemyMessage.RemoveReactionAsync(r.Emote, r.User.Value));
                    reactions.Remove(r);
                }
            });
            await Task.WhenAll(tasks);
        }

        protected virtual async Task WriteEnemiesInit()
        {
            var tasks = new List<Task>
            {
                WriteEnemyEmbeds(),
                WriteEnemyReactions()
            };
            await Task.WhenAll(tasks);
        }

        protected virtual async Task WriteEnemyEmbeds()
        {
            var tasks = new List<Task>();

            Teams.Values.ToList().ForEach(V =>
            {
                var e = GetTeamAsEnemyEmbedBuilder(V.enemies);
                if (V.EnemyMessage.Embeds.Count == 0 || !V.EnemyMessage.Embeds.FirstOrDefault().ToEmbedBuilder().AllFieldsEqual(e))
                {
                    tasks.Add(V.EnemyMessage.ModifyAsync(m =>
                    {
                        m.Content = ""; m.Embed = e.Build();
                    }));
                }
            });
            await Task.WhenAll(tasks);
        }

        protected virtual EmbedBuilder GetTeamAsEnemyEmbedBuilder(Team team)
        {
            var e = new EmbedBuilder();
            if (Battle.SizeTeamB > 0)
            {
                e.WithThumbnailUrl(Battle.GetTeam(team).FirstOrDefault().ImgUrl);
            }
            var i = 1;
            foreach (ColossoFighter fighter in Battle.GetTeam(team))
            {
                e.AddField($"{numberEmotes[i]} {fighter.ConditionsToString()}", $"{fighter.Name}", true);
                i++;
            }
            return e;
        }

        protected virtual async Task WriteEnemyReactions()
        {
            var tasks = new List<Task>();
            Teams.Values.ToList().ForEach(async V =>
            {
                var msg = V.EnemyMessage;
                var oldReactionCount = msg.Reactions.Where(k => numberEmotes.Contains(k.Key.Name)).Count();
                await msg.RemoveAllReactionsAsync();

                if (Battle.GetTeam(V.enemies).Count > 1)
                {
                    tasks.Add(msg.AddReactionsAsync(
                        numberEmotes
                        .Skip(1)
                        .Take(Battle.GetTeam(V.enemies).Count)
                        .Select(s => new Emoji(s))
                        .ToArray()));
                }
            });
            await Task.WhenAll(tasks);
        }

        protected virtual async Task WriteStatusInit()
        {
            await WriteStatus();
        }

        protected virtual async Task WriteSummonsInit()
        {
            _ = WriteSummonsReactions();
            await WriteSummons();
        }

        protected virtual EmbedBuilder GetDjinnEmbedBuilder(PvPTeamCollector V)
        {
            var allDjinn = V.PlayerMessages.Values.SelectMany(p => p.Moves.OfType<Djinn>()).ToList();
            var standbyDjinn = allDjinn.Where(d => d.State == DjinnState.Standby);
            var recoveryDjinn = allDjinn.Where(d => d.State == DjinnState.Recovery);
            if (allDjinn.Count == 0) return null;
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
            Teams.Values.ToList().ForEach(V =>
            {
                _ = V.SummonsMessage.AddReactionsAsync(V.Factory.PossibleSummons.Select(s => s.GetEmote()).ToArray());
            });
            await Task.CompletedTask;
        }

        protected virtual async Task WriteSummons()
        {
            Teams.Values.ToList().ForEach(V =>
            {
                var tasks = new List<Task>();
                var embed = GetDjinnEmbedBuilder(V);
                if (embed != null && (V.SummonsMessage.Embeds.Count == 0 || !V.SummonsMessage.Embeds.FirstOrDefault().ToEmbedBuilder().AllFieldsEqual(embed)))
                {
                    _ = V.SummonsMessage.ModifyAsync(m => m.Embed = embed.Build());
                }

                var validReactions = reactions.Where(r => r.MessageId == V.SummonsMessage.Id).ToList();
                foreach (var r in validReactions)
                {
                    tasks.Add(V.SummonsMessage.RemoveReactionAsync(r.Emote, r.User.Value));
                    reactions.Remove(r);
                }
            });
            await Task.CompletedTask;
        }
        protected virtual async Task WriteStatus()
        {
            List<Task> tasks = new List<Task>();
            Teams.Values.ToList().ForEach(async V =>
            {
                if (Battle.log.Count > 0 && Battle.turn > 0)
                {
                    if (V.StatusMessage == null)
                    {
                        V.StatusMessage = await V.teamChannel.SendMessageAsync(Battle.log.Aggregate("", (s, l) => s += l + "\n"));
                    }
                    else
                    {
                        tasks.Add(V.StatusMessage.ModifyAsync(c => c.Content = Battle.log.Aggregate("", (s, l) => s += l + "\n")));
                    }
                }
                else
                {
                    if (V.StatusMessage == null)
                    {
                        string msg = V.PlayerMessages
                            .Aggregate("", (s, v) => s += $"<@{v.Value.avatar.ID}>, ");
                        V.StatusMessage = await V.teamChannel.SendMessageAsync($"{msg}get in position!");
                    }
                }
            });
            await Task.WhenAll(tasks);
        }

        protected virtual async Task WritePlayers()
        {
            var tasks = new List<Task>();
            Teams.Values.ToList().ForEach((Action<PvPTeamCollector>)(async V =>
            {
                int i = 1;
                foreach (KeyValuePair<IUserMessage, PlayerFighter> k in V.PlayerMessages)
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

                    if (fighter is PlayerFighter fighter1 && fighter.AutoTurnsInARow >= 2)
                    {
                        var ping = await msg.Channel.SendMessageAsync($"<@{fighter1.avatar.ID}>");
                        await ping.DeleteAsync();
                    }
                    i++;
                }
            }));
            await Task.WhenAll(tasks);
        }

        protected virtual async Task WritePlayersInit()
        {
            var tasks = new List<Task>();
            Teams.Values.ToList().ForEach(V =>
            {
                int i = 1;
                foreach (KeyValuePair<IUserMessage, PlayerFighter> k in V.PlayerMessages)
                {
                    var msg = k.Key;
                    var fighter = k.Value;
                    List<IEmote> emotes = new List<IEmote>();
                    if (V.PlayerMessages.Count > 1)
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
            });
            tasks.Add(WritePlayers());
            await Task.WhenAll(tasks);
        }
    }
}