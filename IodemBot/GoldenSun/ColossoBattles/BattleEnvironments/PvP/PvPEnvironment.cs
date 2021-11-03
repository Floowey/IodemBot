using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using IodemBot.Core.Leveling;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.ColossoBattles
{
    public abstract class PvPEnvironment : BattleEnvironment
    {
        private readonly uint _playersToStartB = 4;
        private readonly List<SocketGuildUser> _playersWithBRole = new();
        public IRole TeamBRole;

        protected Dictionary<Team, PvPTeamCollector> Teams = new()
        {
            { Team.A, new PvPTeamCollector { Team = Team.A, Enemies = Team.B } },
            { Team.B, new PvPTeamCollector { Team = Team.B, Enemies = Team.A } }
        };

        public PvPEnvironment(ColossoBattleService battleService, string name, ITextChannel lobbyChannel,
            bool isPersistent, ITextChannel teamAChannel, ITextChannel teamBChannel, IRole teamBRole,
            uint playersToStart = 3, uint playersToStartB = 3) : base(battleService, name, lobbyChannel, isPersistent)
        {
            PlayersToStart = playersToStart;
            _playersToStartB = playersToStartB;
            this.TeamBRole = teamBRole;
            Teams[Team.A].TeamChannel = teamAChannel;
            Teams[Team.B].TeamChannel = teamBChannel;
            Initialize();
        }

        internal override ulong[] ChannelIds => new[] { Teams[Team.A].TeamChannel.Id, Teams[Team.B].TeamChannel.Id };

        private async void Initialize()
        {
            var a = Teams[Team.A];
            var b = Teams[Team.B];

            a.EnemyMessage = await a.TeamChannel.SendMessageAsync($"Welcome to {Name}. Join Team A or Team B.");
            _ = a.EnemyMessage.AddReactionsAsync(new IEmote[]
            {
                Emote.Parse("<:Fight_A:592374736479059979>"),
                Emote.Parse("<:Fight_B:592374736248373279>"),
                Emote.Parse("<:Battle:536954571256365096>")
            });
            a.SummonsMessage = await a.TeamChannel.SendMessageAsync("Good Luck!");
            b.EnemyMessage =
                await b.TeamChannel.SendMessageAsync(
                    $"Welcome to {Name}, Team B. Please wait til the battle has started.");
            b.SummonsMessage = await b.TeamChannel.SendMessageAsync("Good Luck!");
        }

        public override async Task Reset(string msg = "")
        {
            Battle = new ColossoBattle();
            var a = Teams[Team.A];
            var b = Teams[Team.B];

            _playersWithBRole.Where(p => p.Roles.Any(r => r.Name == "TeamB")).ToList()
                .ForEach(a => _ = a.RemoveRoleAsync(TeamBRole));

            foreach (var team in new[] { a, b })
            {
                foreach (var k in team.PlayerMessages.Keys)
                {
                    foreach (var d in team.PlayerMessages[k].Moves.OfType<Djinn>())
                    {
                        d.CoolDown = 0;
                        d.Summon(team.PlayerMessages[k]);
                    }

                    await k.DeleteAsync();
                }

                team.Factory.Djinn.Clear();
                team.Factory.Summons.Clear();
                team.PlayerMessages.Clear();

                if (team.StatusMessage != null)
                {
                    _ = team.StatusMessage.DeleteAsync();
                    team.StatusMessage = null;
                }

                if (team.SummonsMessage != null)
                {
                    _ = team.SummonsMessage.ModifyAsync(m =>
                    {
                        m.Content = "Good Luck!";
                        m.Embed = null;
                    });
                    _ = team.SummonsMessage.RemoveAllReactionsAsync();
                }
            }

            if (a.EnemyMessage != null)
            {
                await a.EnemyMessage.RemoveAllReactionsAsync();
                _ = a.EnemyMessage.ModifyAsync(m =>
                {
                    m.Content = $"Welcome to {Name}. Join Team A or Team B.";
                    m.Embed = null;
                });
                _ = a.EnemyMessage.AddReactionsAsync(new IEmote[]
                {
                    Emote.Parse("<:Fight_A:592374736479059979>"),
                    Emote.Parse("<:Fight_B:592374736248373279>"),
                    Emote.Parse("<:Battle:536954571256365096>")
                });
            }

            if (b.EnemyMessage != null)
            {
                _ = b.EnemyMessage.RemoveAllReactionsAsync();
                _ = b.EnemyMessage.ModifyAsync(m =>
                {
                    m.Content = $"Welcome to {Name}, Team B. Please wait til the battle has started.";
                    m.Embed = null;
                });
            }

            if (AutoTurn != null) AutoTurn.Dispose();
            if (ResetIfNotActive != null) ResetIfNotActive.Dispose();
            AutoTurn = new Timer
            {
                Interval = 60000,
                AutoReset = false,
                Enabled = false
            };
            AutoTurn.Elapsed += TurnTimeElapsed;
            ResetIfNotActive = new Timer
            {
                Interval = 240000,
                AutoReset = false,
                Enabled = false
            };
            ResetIfNotActive.Elapsed += BattleWasNotStartedInTime;

            Console.WriteLine("Battle was reset.");
        }

        private async void BattleWasNotStartedInTime(object sender, ElapsedEventArgs e)
        {
            await Reset("Not started in time");
        }

        private async void TurnTimeElapsed(object sender, ElapsedEventArgs e)
        {
            _ = ProcessTurn(true);
            await Task.CompletedTask;
        }

        protected override async Task GameOver()
        {
            var winners = Battle.GetTeam(Battle.GetWinner());
            var losers = winners.First().Battle.GetTeam(winners.First().enemies);

            winners.ConvertAll(s => (PlayerFighter)s).ForEach(async p =>
               await ServerGames.UserWonPvP(UserAccountProvider.GetById(p.Id), LobbyChannel, winners.Count,
                   losers.Count));

            _ = WriteGameOver();
            await Task.CompletedTask;
        }

        private async Task WriteGameOver()
        {
            await Task.Delay(5000);
            var winners = Battle.GetTeam(Battle.GetWinner());
            var text = $"{winners.FirstOrDefault()?.Name ?? "Nobodys!?"}'s party wins! Battle will reset shortly.";

            _ = Teams[Team.A].StatusMessage.ModifyAsync(m =>
            {
                m.Content = text;
                m.Embed = null;
            });
            _ = Teams[Team.B].StatusMessage.ModifyAsync(m =>
            {
                m.Content = text;
                m.Embed = null;
            });

            await Task.Delay(5000);
            _ = Reset($"Game over: {text}");
        }

        protected override async Task ProcessReaction(IUserMessage cache, IMessageChannel channel,
            SocketReaction reaction)
        {
            try
            {
                if (reaction.User.Value.IsBot) return;
                if (channel.Id != Teams[Team.A].TeamChannel.Id && channel.Id != Teams[Team.B].TeamChannel.Id) return;
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

                if (reaction.Emote.Name == "Battle")
                {
                    _ = StartBattle();
                    return;
                }

                if (!Battle.IsActive) return;

                Teams.Values.ToList().ForEach(async v =>
                {
                    var statusMessage = v.StatusMessage;
                    var playerMessages = v.PlayerMessages;
                    var enemyMessage = v.EnemyMessage;
                    var summonsMessage = v.SummonsMessage;
                    if (channel.Id != v.TeamChannel.Id) return;
                    IUserMessage c = null;
                    if (statusMessage.Id == reaction.MessageId) c = statusMessage;
                    if (enemyMessage.Id == reaction.MessageId) c = enemyMessage;
                    if (summonsMessage.Id == reaction.MessageId) c = summonsMessage;
                    if (playerMessages.Keys.Any(k => k.Id == reaction.MessageId))
                        c = playerMessages.Keys.First(k => k.Id == reaction.MessageId);

                    if (c == null)
                    {
                        c = (IUserMessage)await channel.GetMessageAsync(reaction.MessageId);
                        _ = c.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        Console.WriteLine("No matching Message for User found.");
                        return;
                    }

                    if (!Battle.IsActive)
                    {
                        _ = c.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        Console.WriteLine("Battle not active.");
                        return;
                    }

                    if (Battle.TurnActive)
                    {
                        _ = c.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        Console.WriteLine("Not so fast");
                        return;
                    }

                    if (reaction.Emote.Name == "🔄")
                    {
                        await c.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        AutoTurn.Stop();
                        Task.WaitAll(playerMessages.Select(m => m.Key.RemoveAllReactionsAsync())
                            .Append(enemyMessage.RemoveAllReactionsAsync()).ToArray());

                        _ = WriteBattleInit();
                        AutoTurn.Start();
                        return;
                    }

                    if (reaction.Emote.Name == "⏸️")
                    {
                        AutoTurn.Stop();
                        return;
                    }

                    if (reaction.Emote.Name == "▶")
                    {
                        AutoTurn.Start();
                        return;
                    }

                    if (reaction.Emote.Name == "⏩")
                    {
                        _ = ProcessTurn(true);
                        return;
                    }

                    var curPlayer = playerMessages.Values.FirstOrDefault(p => p.Id == reaction.User.Value.Id);
                    if (curPlayer == null)
                    {
                        _ = c.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        Console.WriteLine("Player not in this room.");
                        return;
                    }

                    var correctId = playerMessages.Keys.First(key => playerMessages[key].Id == curPlayer.Id).Id;

                    if (!NumberEmotes.Contains(reaction.Emote.Name))
                        if (reaction.MessageId != enemyMessage.Id && reaction.MessageId != summonsMessage.Id &&
                            reaction.MessageId != correctId)
                        {
                            _ = c.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                            Console.WriteLine("Didn't click on own message.");
                            return;
                        }

                    if (!curPlayer.Select(reaction.Emote))
                    {
                        _ = c.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        Console.WriteLine("Couldn't select that move.");
                        return;
                    }

                    Reactions.Add(reaction);
                });

                _ = ProcessTurn(false);
            }
            catch (Exception e)
            {
                Console.WriteLine("Colosso Turn Processing Error: {reaction.Emote}" + e);
                File.WriteAllText($"Logs/Crashes/Error_{DateTime.Now.Date}.log", e.ToString());
            }

            await Task.CompletedTask;
        }

        protected virtual async Task AddPlayer(SocketReaction reaction, Team team)
        {
            if (Teams[Team.A].PlayerMessages.Values.Any(s => s.Id == reaction.UserId)) return;
            if (Teams[Team.B].PlayerMessages.Values.Any(s => s.Id == reaction.UserId)) return;
            var player = (SocketGuildUser)reaction.User.Value;
            if (team == Team.B)
            {
                await player.AddRoleAsync(TeamBRole);
                _playersWithBRole.Add(player);
            }

            var playerAvatar = EntityConverter.ConvertUser(player);

            await AddPlayer(playerAvatar, team);
        }

        public override async Task AddPlayer(UserAccount user, Team team = Team.A)
        {
            var factory = Teams[team].Factory;
            var p = factory.CreatePlayerFighter(user);
            await AddPlayer(p, team);
        }

        public override async Task AddPlayer(PlayerFighter player, Team team)
        {
            if (Battle.IsActive) return;

            Battle.AddPlayer(player, team);

            var playerMsg = await Teams[team].TeamChannel.SendMessageAsync($"{player.Name} wants to battle!");
            Teams[team].PlayerMessages.Add(playerMsg, player);
            ResetIfNotActive.Start();

            if (Teams[Team.A].PlayerMessages.Count == PlayersToStart &&
                Teams[Team.B].PlayerMessages.Count == _playersToStartB) await StartBattle();
        }

        public override Task<(bool Success, string Message)> CanPlayerJoin(UserAccount user, Team team = Team.A)
        {
            if (Battle.GetTeam(team).Count >= PlayersToStart)
                return Task.FromResult((false, "This team is already full."));

            return Task.FromResult((true, (string)null));
        }

        public override async Task StartBattle()
        {
            if (Battle.IsActive) return;

            if (Battle.SizeTeamA == 0 || Battle.SizeTeamB == 0) return;

            foreach (var v in Teams.Values)
                v.PlayerMessages.Values.ToList().ForEach(p => p.Moves.AddRange(v.Factory.PossibleSummons));

            ResetIfNotActive.Stop();
            Battle.Start();
            await WriteBattleInit();
            AutoTurn.Start();
        }

        protected override async Task WriteBattle()
        {
            var delay = Global.Client.Latency / 2;
            try
            {
                await Task.Delay(delay);
                await WriteStatus();
                await Task.Delay(delay);
                await WriteSummons();
                await Task.Delay(delay);
                await WriteEnemies();
                await Task.Delay(delay);
                await WritePlayers();
                await Task.Delay(delay);
            }
            catch (HttpException e)
            {
                Console.WriteLine("Failed drawing Battle, retrying." + e);
                Battle.Log.Add("Failed drawing Battle, retrying.");
                await WriteStatus();
                await Task.Delay(delay);
                await WriteSummons();
                await Task.Delay(delay);
                await WriteEnemies();
                await Task.Delay(delay);
                await WritePlayers();
            }
            catch (TimeoutException e)
            {
                Console.WriteLine("Timed out while drawing Battle, retrying." + e);
                Battle.Log.Add("Timed out while drawing Battle, retrying in 30s.");
                await Task.Delay(300000);
                await WriteBattle();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception while writing Battle: " + e);
                throw new Exception("Exception while writing Battle: ", e);
            }
        }

        protected override async Task WriteBattleInit()
        {
            var delay = Global.Client.Latency / 2;
            try
            {
                await WriteStatusInit();
                await Task.Delay(delay);
                await WriteSummonsInit();
                await Task.Delay(delay);
                await WriteEnemiesInit();
                await Task.Delay(delay);
                await WritePlayersInit();
            }
            catch (HttpException e)
            {
                Console.WriteLine("Failed drawing Battle, retrying: " + e);
                Battle.Log.Add("Failed drawing Battle, retrying.");
                await WriteStatusInit();
                await Task.Delay(delay);
                await WriteSummonsInit();
                await Task.Delay(delay);
                await WriteEnemiesInit();
                await Task.Delay(delay);
                await WritePlayersInit();
            }
            catch (TimeoutException e)
            {
                Console.WriteLine("Timed out while drawing Battle, retrying: " + e);
                Battle.Log.Add("Timed out while drawing Battle, retrying in 30s");
                await Task.Delay(300000);
                await WriteBattleInit();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception while writing Battle:" + e);
                throw new Exception("Exception while writing Battle", e);
            }
        }

        protected virtual async Task WriteEnemies()
        {
            var tasks = new List<Task>
            {
                WriteEnemyEmbeds()
            };

            Teams.Values.ToList().ForEach(v =>
            {
                var validReactions = Reactions.Where(r => r.MessageId == v.EnemyMessage.Id).ToList();
                foreach (var r in validReactions)
                {
                    tasks.Add(v.EnemyMessage.RemoveReactionAsync(r.Emote, r.User.Value));
                    Reactions.Remove(r);
                }
            });
            await Task.WhenAll(tasks);
        }

        protected virtual async Task WriteEnemiesInit()
        {
            var tasks = new List<Task>
            {
                WriteEnemyEmbeds()
            };
            await Task.WhenAll(tasks);
        }

        protected virtual async Task WriteEnemyEmbeds()
        {
            var tasks = new List<Task>();

            Teams.Values.ToList().ForEach(v =>
            {
                var e = GetTeamAsEnemyEmbedBuilder(v.Enemies);
                if (v.EnemyMessage.Embeds.Count == 0 ||
                    !v.EnemyMessage.Embeds.FirstOrDefault().ToEmbedBuilder().AllFieldsEqual(e))
                    tasks.Add(v.EnemyMessage.ModifyAsync(m =>
                    {
                        m.Content = "";
                        m.Embed = e.Build();
                    }));
            });
            await Task.WhenAll(tasks);
        }

        protected virtual EmbedBuilder GetTeamAsEnemyEmbedBuilder(Team team)
        {
            var e = new EmbedBuilder();
            if (Battle.SizeTeamB > 0) e.WithThumbnailUrl(Battle.GetTeam(team).FirstOrDefault().ImgUrl);
            var i = 1;
            foreach (var fighter in Battle.GetTeam(team))
            {
                e.AddField($"{NumberEmotes[i]} {fighter.ConditionsToString()}".Trim(), $"{fighter.Name}", true);
                i++;
            }

            return e;
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

        protected virtual EmbedBuilder GetDjinnEmbedBuilder(PvPTeamCollector v)
        {
            var allDjinn = v.PlayerMessages.Values.SelectMany(p => p.Moves.OfType<Djinn>()).ToList();
            var standbyDjinn = allDjinn.Where(d => d.State == DjinnState.Standby);
            var recoveryDjinn = allDjinn.Where(d => d.State == DjinnState.Recovery);
            if (allDjinn.Count == 0) return null;

            var embed = new EmbedBuilder();
            //.WithThumbnailUrl("https://cdn.discordapp.com/attachments/497696510688100352/640300243820216336/unknown.png");

            foreach (var el in new[] { Element.Venus, Element.Mars, Element.Jupiter, Element.Mercury })
                if (allDjinn.OfElement(el).Any())
                {
                    var standby = string.Join(" ", standbyDjinn.OfElement(el).Select(d => d.Emote));
                    var recovery = string.Join(" ", recoveryDjinn.OfElement(el).Select(d => d.Emote));
                    embed.WithColor(Colors.Get(standbyDjinn.Select(e => e.Element.ToString()).ToList()));

                    embed.AddField(Emotes.GetIcon(el), ($"{standby}" +
                                                        $"{(!standby.IsNullOrEmpty() && !recovery.IsNullOrEmpty() ? "\n" : "\u200b")}" +
                                                        $"{(recovery.IsNullOrEmpty() ? "" : $"({recovery})")}").Trim(),
                        true);
                    if (embed.Fields.Count == 2 || embed.Fields.Count == 5) embed.AddField("\u200b", "\u200b", true);
                }

            return embed;
        }

        protected virtual async Task WriteSummonsReactions()
        {
            Teams.Values.ToList().ForEach(v =>
            {
                _ = v.SummonsMessage.AddReactionsAsync(
                    v.Factory.PossibleSummons.Select(s => s.GetEmote()).ToArray());
            });
            await Task.CompletedTask;
        }

        protected virtual async Task WriteSummons()
        {
            Teams.Values.ToList().ForEach(v =>
            {
                var tasks = new List<Task>();
                var embed = GetDjinnEmbedBuilder(v);
                if (embed != null && (v.SummonsMessage.Embeds.Count == 0 ||
                                      !v.SummonsMessage.Embeds.FirstOrDefault().ToEmbedBuilder().AllFieldsEqual(embed)))
                    _ = v.SummonsMessage.ModifyAsync(m => m.Embed = embed.Build());

                var validReactions = Reactions.Where(r => r.MessageId == v.SummonsMessage.Id).ToList();
                foreach (var r in validReactions)
                {
                    tasks.Add(v.SummonsMessage.RemoveReactionAsync(r.Emote, r.User.Value));
                    Reactions.Remove(r);
                }
            });
            await Task.CompletedTask;
        }

        protected virtual async Task WriteStatus()
        {
            var tasks = new List<Task>();
            Teams.Values.ToList().ForEach(async v =>
            {
                if (Battle.Log.Count > 0 && Battle.TurnNumber > 0)
                {
                    if (v.StatusMessage == null)
                        v.StatusMessage =
                            await v.TeamChannel.SendMessageAsync(Battle.Log.Aggregate("", (s, l) => s += l + "\n"));
                    else
                        tasks.Add(v.StatusMessage.ModifyAsync(c =>
                            c.Content = Battle.Log.Aggregate("", (s, l) => s += l + "\n")));
                }
                else
                {
                    if (v.StatusMessage == null)
                    {
                        var msg = v.PlayerMessages
                            .Aggregate("", (s, v) => s += $"<@{v.Value.Id}>, ");
                        v.StatusMessage = await v.TeamChannel.SendMessageAsync($"{msg}get in position!");
                    }
                }
            });
            await Task.WhenAll(tasks);
        }

        protected virtual async Task WritePlayers()
        {
            var tasks = new List<Task>();
            Teams.Values.ToList().ForEach(async v =>
            {
                var i = 1;
                foreach (var k in v.PlayerMessages)
                {
                    var msg = k.Key;
                    var embed = new EmbedBuilder();
                    var fighter = k.Value;

                    var validReactions = Reactions.Where(r => r.MessageId == msg.Id).ToList();
                    foreach (var r in validReactions)
                    {
                        tasks.Add(msg.RemoveReactionAsync(r.Emote, r.User.Value));
                        Reactions.Remove(r);
                    }

                    embed.WithThumbnailUrl(fighter.ImgUrl);
                    embed.WithColor(Colors.Get(fighter.Moves.OfType<Psynergy>().Select(p => p.Element.ToString())
                        .ToArray()));
                    embed.AddField($"{NumberEmotes[i]}{fighter.ConditionsToString()}", fighter.Name, true);
                    embed.AddField("HP", $"{fighter.Stats.HP} / {fighter.Stats.MaxHP}", true);
                    embed.AddField("PP", $"{fighter.Stats.PP} / {fighter.Stats.MaxPP}", true);
                    var s = new List<string>();
                    foreach (var m in fighter.Moves)
                        if (m is Psynergy p)
                        {
                            s.Add($"{m.Emote} {m.Name} {p.PpCost}");
                        }
                        else if (m is Summon summon)
                        {
                        }
                        else
                        {
                            s.Add($"{m.Emote} {m.Name}");
                        }

                    embed.AddField("Psynergy", string.Join(" | ", s));

                    if (msg.Embeds.Count == 0 || !msg.Embeds.FirstOrDefault().ToEmbedBuilder().AllFieldsEqual(embed))
                        tasks.Add(msg.ModifyAsync(m =>
                        {
                            m.Content = "";
                            m.Embed = embed.Build();
                        }));

                    if (fighter is PlayerFighter fighter1 && fighter.AutoTurnsInARow >= 2)
                    {
                        var ping = await msg.Channel.SendMessageAsync($"<@{fighter1.Id}>");
                        await ping.DeleteAsync();
                    }

                    i++;
                }
            });
            await Task.WhenAll(tasks);
        }

        protected virtual async Task WritePlayersInit()
        {
            var tasks = new List<Task>();
            Teams.Values.ToList().ForEach(v =>
            {
                var i = 1;
                foreach (var k in v.PlayerMessages)
                {
                    var msg = k.Key;
                    var fighter = k.Value;
                    var emotes = new List<IEmote>();
                    if (v.PlayerMessages.Count > 1) emotes.Add(new Emoji(NumberEmotes[i]));
                    foreach (var m in fighter.Moves)
                        if (!(m is Summon))
                            emotes.Add(m.GetEmote());
                    //emotes.RemoveAll(e => msg.Reactions.Any(r => r.Key.Name.Equals(e.Name, StringComparison.InvariantCultureIgnoreCase)));
                    //tasks.Add(msg.AddReactionsAsync(emotes.ToArray()));
                    tasks.Add(
                        msg.AddReactionsAsync(
                            emotes.Except(msg.Reactions.Keys).ToArray()
                        )
                    );
                    i++;
                }
            });
            tasks.Add(WritePlayers());
            await Task.WhenAll(tasks);
        }

        public override bool IsUsersMessage(PlayerFighter player, IUserMessage message)
        {
            if (Teams[Team.A].PlayerMessages.TryGetValue(message, out var pf))
                return pf.Id == player.Id;
            if (Teams[Team.B].PlayerMessages.TryGetValue(message, out pf))
                return pf.Id == player.Id;

            return Teams[Team.A].SummonsMessage.Id == message.Id || Teams[Team.B].SummonsMessage.Id == message.Id;
        }

        protected class PvPTeamCollector
        {
            public Team Enemies;
            public IUserMessage EnemyMessage;

            public PlayerFighterFactory Factory = new()
            { LevelOption = LevelOption.SetLevel, SetLevel = 60, DjinnOption = DjinnOption.Unique };

            public Dictionary<IUserMessage, PlayerFighter> PlayerMessages = new();
            public IUserMessage StatusMessage;
            public IUserMessage SummonsMessage;
            public Team Team;
            public ITextChannel TeamChannel;
        }
    }
}