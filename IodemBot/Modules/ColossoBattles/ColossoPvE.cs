using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using IodemBot.Core.Leveling;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace IodemBot.Modules.ColossoBattles
{
    [Group("colosso")]
    public class ColossoPvE : ModuleBase<SocketCommandContext>
    {
        private static string[] numberEmotes = new string[] { "\u0030\u20E3", "1⃣", "\u0032\u20E3", "\u0033\u20E3", "\u0034\u20E3", "\u0035\u20E3",
            "\u0036\u20E3", "\u0037\u20E3", "\u0038\u20E3", "\u0039\u20E3" };

        //private static ulong lobbyChannelId;
        private static List<BattleCollector> battles;

        private static SocketTextChannel textChannel;

        static ColossoPvE()
        {
            //lobbyChannelId = 528637030825984000;
            Global.Client.ReactionAdded += ReactionAdded;
            battles = new List<BattleCollector>();
        }

        [Command("setup")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task SetupColosso()
        {
            textChannel = (SocketTextChannel)Context.Channel;
            await Context.Message.DeleteAsync();
            _ = Setup();
        }

        private async Task Setup()
        {
            battles = new List<BattleCollector>();
            var b = await GetBattleCollector(Context, "Bronze", BattleDifficulty.Easy);
            battles.Add(b);
            b = await GetBattleCollector(Context, "Silver", BattleDifficulty.Medium);
            battles.Add(b);
            b = await GetBattleCollector(Context, "Gold", BattleDifficulty.Hard);
            battles.Add(b);
            b = await GetBattleCollector(Context, "Showdown", BattleDifficulty.Easy);
            b.isEndless = true;
            battles.Add(b);
        }

        [Command("reset")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task Reset(string name)
        {
            await Context.Message.DeleteAsync();
            var a = battles.Where(b => b.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (a != null)
            {
                _ = a.Reset();
            }
        }

        [Command("setEnemy")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task SetEnemy(BattleDifficulty diff, [Remainder] string enemy)
        {
            await Context.Message.DeleteAsync();
            var a = battles.Where(b => b.diff == diff).FirstOrDefault();
            if (a != null)
            {
                a.SetEnemy(enemy);
            }
        }

        private async Task<BattleCollector> GetBattleCollector(SocketCommandContext Context, string Name, BattleDifficulty diff)
        {
            var b = new BattleCollector
            {
                Name = Name,
                diff = diff
            };
            var channel = await Context.Guild.GetOrCreateTextChannelAsync("colosso-" + b.Name);
            await channel.ModifyAsync(c =>
            {
                c.CategoryId = ((ITextChannel)Context.Channel).CategoryId;
                c.Position = ((ITextChannel)Context.Channel).Position + battles.Count + 1;
            });
            await channel.SyncPermissionsAsync();
            var messages = await channel.GetMessagesAsync(100).FlattenAsync();
            await channel.DeleteMessagesAsync(messages);
            b.BattleChannel = channel;

            b.EnemyMsg = await b.BattleChannel.SendMessageAsync($"Welcome to {b.Name} Battle!\n\nReact with <:Fight:536919792813211648> to join the {b.Name} Battle and press <:Battle:536954571256365096> when you are ready to battle!");
            await b.Reset();
            return b;
        }

        internal static async Task TryAddPlayer(SocketReaction reaction)
        {
            var battleCol = battles.Where(s => s.EnemyMsg.Id == reaction.MessageId).FirstOrDefault();
            if (battleCol == null)
            {
                return;
            }

            if (battleCol.Battle.isActive)
            {
                return;
            }

            if (battleCol.Messages.Values.Where(v => v is PlayerFighter).Where(s => ((PlayerFighter)s).avatar.ID == reaction.UserId).Any())
            {
                return;
            }

            SocketGuildUser player = (SocketGuildUser)reaction.User.Value;
            var playerAvatar = UserAccounts.GetAccount(player);

            var p = new PlayerFighter(player);
            battleCol.Battle.AddPlayer(p, ColossoBattle.Team.A);

            if (playerAvatar.Inv.GetGear(AdeptClassSeriesManager.GetClassSeries(playerAvatar).Archtype).Any(i => i.Name == "Lure Cap"))
            {
                battleCol.LureCaps++;
                battleCol.SetRandomEnemies(ColossoBattle.Team.B);
            }

            if (battleCol.Name == "Bronze")
            {
                if (playerAvatar.LevelNumber < 10 && battleCol.Messages.Count == 0)
                {
                    battleCol.diff = BattleDifficulty.Tutorial;
                    battleCol.SetRandomEnemies(ColossoBattle.Team.B);
                }
                else
                {
                    if (battleCol.diff != BattleDifficulty.Easy)
                    {
                        battleCol.diff = BattleDifficulty.Easy;
                        battleCol.SetRandomEnemies(ColossoBattle.Team.B);
                    }
                }
            }

            var playerMsg = await battleCol.BattleChannel.SendMessageAsync($"{player.DisplayName()} wants to battle!");
            battleCol.Messages.Add(playerMsg, p);

            if (battleCol.Messages.Count == battleCol.PlayersToStart)
            {
                await TryStartBattle(reaction);
            }
        }

        internal static async Task ReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.User.Value.IsBot)
            {
                return;
            }

            if (reaction.Emote.Name == "Fight")
            {
                await TryAddPlayer(reaction);
                return;
            }
            else if (reaction.Emote.Name == "Battle")
            {
                await TryStartBattle(reaction);
                return;
            }

            var curBattle = battles.Where(b => b.BattleChannel.Id == reaction.Channel.Id).FirstOrDefault();
            if (curBattle == null)
            {
                return;
            }

            RestUserMessage c;
            if (cache.HasValue)
            {
                c = (RestUserMessage)cache.Value;
            }
            else
            {
                channel.GetCachedMessage(reaction.MessageId);
                c = (RestUserMessage)await channel.GetMessageAsync(reaction.MessageId);
            }
            _ = curBattle.ProcessReaction(reaction, c);
        }

        private static async Task TryStartBattle(SocketReaction reaction)
        {
            var battleCol = battles.Where(s => s.EnemyMsg.Id == reaction.MessageId).FirstOrDefault();
            if (battleCol.Equals(null))
            {
                return;
            }

            if (battleCol.Battle.isActive)
            {
                return;
            }

            if (battleCol.Battle.sizeTeamA == 0)
            {
                return;
            }

            battleCol.Battle.Start();
            _ = battleCol.WriteBattleInit();
            await Task.CompletedTask;
        }

        public enum BattleDifficulty { Tutorial = 0, Easy = 1, Medium = 2, MediumRare = 3, Hard = 4, Adept = 5 };

        internal class BattleCollector
        {
            internal ColossoBattle Battle { get; set; }
            internal ITextChannel BattleChannel { get; set; }
            internal IUserMessage EnemyMsg { get; set; }
            internal IUserMessage StatusMsg { get; set; }
            internal Dictionary<IUserMessage, ColossoFighter> Messages { get; set; }
            internal uint PlayersToStart = 4;
            internal BattleDifficulty diff;
            internal string Name;
            internal Timer autoTurn;
            internal bool isEndless = false;
            internal int winsInARow = 0;
            internal int LureCaps = 0;
            internal readonly int stageLength = 12;
            private List<SocketReaction> reactions = new List<SocketReaction>();

            internal async Task Reset()
            {
                Battle = new ColossoBattle();

                if (autoTurn != null)
                {
                    autoTurn.Dispose();
                }

                if (Messages != null)
                {
                    foreach (var k in Messages.Keys)
                    {
                        await k.DeleteAsync();
                    }
                }

                Messages = new Dictionary<IUserMessage, ColossoFighter>();

                if (EnemyMsg != null)
                {
                    _ = EnemyMsg.ModifyAsync(c => { c.Content = $"Welcome to {Name} Battle!\n\nReact with <:Fight:536919792813211648> to join the {Name} Battle and press <:Battle:536954571256365096> when you are ready to battle!"; c.Embed = null; });
                    await EnemyMsg.RemoveAllReactionsAsync();
                    _ = EnemyMsg.AddReactionsAsync(new IEmote[]
                    {
                        Emote.Parse("<:Fight:536919792813211648>"),
                        Emote.Parse("<:Battle:536954571256365096>")
                    });
                }
                if (StatusMsg != null)
                {
                    _ = StatusMsg.DeleteAsync();
                    StatusMsg = null;
                }
                winsInARow = 0;
                LureCaps = 0;
                SetRandomEnemies(ColossoBattle.Team.B);
                Console.WriteLine($"Up against {Battle.TeamB.First().name}");

                autoTurn = new Timer()
                {
                    Interval = 25000,
                    AutoReset = false,
                    Enabled = false
                };
                autoTurn.Elapsed += OnTimerTicked;

                Console.WriteLine("Battle was reset.");
            }

            private async void OnTimerTicked(object sender, ElapsedEventArgs e)
            {
                _ = ProcessTurn(forced: true);
                await Task.CompletedTask;
            }

            internal void SetRandomEnemies(ColossoBattle.Team team)
            {
                Battle.GetTeam(team).Clear();
                EnemiesDatabase.GetRandomEnemies(diff).ForEach(f =>
                {
                    f.stats *= (1 + ((double)winsInARow / 50) % (winsInARow < 4 * stageLength ? stageLength : 1));
                    Battle.AddPlayer(f, ColossoBattle.Team.B);
                }
                );

                for (int i = 0; i < LureCaps; i++)
                {
                    if (Battle.GetTeam(team).Count < 9)
                    {
                        Battle.AddPlayer(EnemiesDatabase.GetRandomEnemies(diff).Random(), team);
                    }
                }
            }

            internal async Task ProcessReaction(SocketReaction reaction, RestUserMessage c)
            {
                var tryMsg = Messages.Keys.Where(k => k.Id == reaction.MessageId).FirstOrDefault();

                if (EnemyMsg.Id == reaction.MessageId)
                {
                    tryMsg = StatusMsg;
                }
                if (tryMsg == null)
                {
                    await c.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                    Console.WriteLine("No matching Message for user found.");
                    return;
                }

                if (!Battle.isActive)
                {
                    await c.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                    Console.WriteLine("Battle not active.");
                    return;
                }

                if (reaction.Emote.Name == "🔄")
                {
                    await c.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                    autoTurn.Stop();
                    foreach (KeyValuePair<IUserMessage, ColossoFighter> k in Messages)
                    {
                        var msg = k.Key;
                        await msg.RemoveAllReactionsAsync();
                    }
                    await WriteBattleInit();
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

                var curPlayer = Messages.Values.Where(p => p.name == ((SocketGuildUser)reaction.User.Value).DisplayName()).FirstOrDefault();
                var correctID = Messages.Keys.Where(key => Messages[key].name == curPlayer.name).First().Id;

                if (!numberEmotes.Contains(reaction.Emote.Name))
                {
                    if (reaction.MessageId != EnemyMsg.Id && reaction.MessageId != correctID)
                    {
                        await c.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        Console.WriteLine("Didn't click on own message.");
                        return;
                    }
                }

                if (!curPlayer.Select(reaction.Emote.Name))
                {
                    await c.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                    Console.WriteLine("Couldn't select that move.");
                    return;
                }
                reactions.Add(reaction);
                _ = ProcessTurn(forced: false);
            }

            internal async Task ProcessTurn(bool forced)
            {
                bool turnProcessed = forced ? Battle.ForceTurn() : Battle.Turn();
                if (turnProcessed)
                {
                    autoTurn.Stop();
                    await WriteBattle();
                    if (Battle.isActive)
                    {
                        autoTurn.Start();
                    }
                    else
                    {
                        await GameOver();
                    }
                };
            }

            internal async Task WriteBattle()
            {
                await WriteStatus();
                await WriteEnemies();
                await WritePlayers();
            }

            private async Task WriteStatus()
            {
                if (Battle.log.Count > 0)
                {
                    if (StatusMsg == null)
                    {
                        StatusMsg = await BattleChannel.SendMessageAsync(Battle.log.Aggregate("", (s, l) => s += l + "\n"));
                    }
                    else
                    {
                        await StatusMsg.ModifyAsync(c => c.Content = Battle.log.Aggregate("", (s, l) => s += l + "\n"));
                    }
                }
                else
                {
                    if (StatusMsg == null)
                    {
                        string msg = Messages.Values
                            .Where(p => p is PlayerFighter)
                            .Aggregate("", (s, v) => s += $"<@{((PlayerFighter)v).avatar.ID}>, ");
                        StatusMsg = await BattleChannel.SendMessageAsync($"{msg}get in position!");
                    }
                }
            }

            private async Task WriteEnemies()
            {
                var e = new EmbedBuilder();
                e.WithThumbnailUrl(Battle.GetTeam(ColossoBattle.Team.B).FirstOrDefault().imgUrl);
                var msg = EnemyMsg;
                var i = 1;
                foreach (ColossoFighter fighter in Battle.GetTeam(ColossoBattle.Team.B))
                {
                    //e.AddField(numberEmotes[i], $"{fighter.name} {fighter.stats.HP}/{fighter.stats.maxHP}", true);
                    e.AddField($"{numberEmotes[i]} {fighter.ConditionsToString()}", $"{fighter.name}", true);
                    i++;
                }
                if (isEndless)
                {
                    EmbedFooterBuilder footerBuilder = new EmbedFooterBuilder();
                    footerBuilder.WithText($"Battle {winsInARow + 1} - {diff}");
                    e.WithFooter(footerBuilder);
                }
                await msg.ModifyAsync(m => m.Embed = e.Build());

                var validReactions = reactions.Where(r => r.MessageId == EnemyMsg.Id).ToList();
                foreach (var r in validReactions)
                {
                    await EnemyMsg.RemoveReactionAsync(r.Emote, r.User.Value);
                    reactions.Remove(r);
                }
            }

            private async Task WritePlayers()
            {
                int i = 1;
                foreach (KeyValuePair<IUserMessage, ColossoFighter> k in Messages)
                {
                    var msg = k.Key;
                    var embed = new EmbedBuilder();
                    var fighter = k.Value;

                    var validReactions = reactions.Where(r => r.MessageId == msg.Id).ToList();
                    foreach (var r in validReactions)
                    {
                        await msg.RemoveReactionAsync(r.Emote, r.User.Value);
                        reactions.Remove(r);
                    }
                    //e.WithAuthor($"{numberEmotes[i]} {fighter.name}");
                    embed.WithThumbnailUrl(fighter.imgUrl);
                    embed.WithColor(Colors.Get(fighter.moves.Where(m => m is Psynergy).Select(m => (Psynergy)m).Select(p => p.element.ToString()).ToArray()));
                    //e.AddField();
                    embed.AddField($"{numberEmotes[i]} {fighter.ConditionsToString()}", fighter.name);
                    embed.AddField("HP", $"{fighter.stats.HP} / {fighter.stats.MaxHP}", true);
                    embed.AddField("PP", $"{fighter.stats.PP} / {fighter.stats.MaxPP}", true);
                    var s = new StringBuilder();
                    foreach (var m in fighter.moves)
                    {
                        if (m is Psynergy)
                        {
                            s.Append($"{m.emote} {m.name} {((Psynergy)m).PPCost} | ");
                        }
                        else
                        {
                            s.Append($"{m.emote} {m.name} | ");
                        }
                    }
                    embed.AddField("Psynergy", s.ToString());

                    await msg.ModifyAsync(m => { m.Content = ""; m.Embed = embed.Build(); });
                    i++;
                }
            }

            internal async Task WriteBattleInit()
            {
                await WriteStatusInit();
                await WriteEnemiesInit();
                await WritePlayersInit();
                autoTurn.Start();
            }

            private async Task WriteStatusInit()
            {
                await WriteStatus();
            }

            private async Task WriteEnemiesInit()
            {
                var e = new EmbedBuilder();
                e.WithThumbnailUrl(Battle.GetTeam(ColossoBattle.Team.B).FirstOrDefault().imgUrl);
                var msg = EnemyMsg;
                var i = 1;
                foreach (ColossoFighter fighter in Battle.GetTeam(ColossoBattle.Team.B))
                {
                    //e.AddField(numberEmotes[i], $"{fighter.name} {fighter.stats.HP}/{fighter.stats.maxHP}", true);
                    e.AddField($"{numberEmotes[i]} {fighter.ConditionsToString()}", $"{fighter.name}", true);
                    i++;
                }
                if (isEndless)
                {
                    EmbedFooterBuilder footerBuilder = new EmbedFooterBuilder();
                    footerBuilder.WithText($"Battle {winsInARow + 1} - {diff}");
                    e.WithFooter(footerBuilder);
                }
                _ = msg.ModifyAsync(m => { m.Content = ""; m.Embed = e.Build(); });
                //var countA = battle.getTeam(ColossoBattle.Team.A).Count;
                var countA = 1;
                var countB = Battle.GetTeam(ColossoBattle.Team.B).Count;
                await msg.RemoveAllReactionsAsync();
                if (countA > 1 || countB > 1)
                {
                    for (int j = 1; j <= Math.Max(countA, countB); j++)
                    {
                        await msg.AddReactionAsync(new Emoji(numberEmotes[j]));
                    }
                }
            }

            private async Task WritePlayersInit()
            {
                int i = 1;
                foreach (KeyValuePair<IUserMessage, ColossoFighter> k in Messages)
                {
                    var msg = k.Key;
                    var embed = new EmbedBuilder();
                    var fighter = k.Value;
                    //e.WithAuthor($"{numberEmotes[i]} {fighter.name}");
                    embed.WithThumbnailUrl(fighter.imgUrl);
                    embed.WithColor(Colors.Get(fighter.moves.Where(m => m is Psynergy).Select(m => (Psynergy)m).Select(p => p.element.ToString()).ToArray()));
                    //e.AddField();
                    embed.AddField($"{numberEmotes[i]} {fighter.ConditionsToString()}", fighter.name);
                    embed.AddField("HP", $"{fighter.stats.HP} / {fighter.stats.MaxHP}", true);
                    embed.AddField("PP", $"{fighter.stats.PP} / {fighter.stats.MaxPP}", true);
                    var s = new StringBuilder();
                    List<IEmote> emotes = new List<IEmote>();
                    if (Messages.Count > 1)
                    {
                        emotes.Add(new Emoji(numberEmotes[i]));
                    }

                    foreach (var m in fighter.moves)
                    {
                        if (m is Psynergy)
                        {
                            s.Append($"{m.emote} {m.name} {((Psynergy)m).PPCost} | ");
                        }
                        else
                        {
                            s.Append($"{m.emote} {m.name} | ");
                        }
                        if (fighter.IsAlive())
                        {
                            IEmote e;
                            try
                            {
                                if (m.emote.StartsWith("<"))
                                {
                                    e = Emote.Parse(m.emote);
                                }
                                else
                                {
                                    e = new Emoji(m.emote);
                                }
                            }
                            catch
                            {
                                e = new Emoji("⛔");
                            }

                            if (m is Psynergy)
                            {
                                if (fighter.stats.PP >= ((Psynergy)m).PPCost)
                                {
                                    emotes.Add(e);
                                }
                            }
                            else
                            {
                                emotes.Add(e);
                            }
                        }
                    }
                    embed.AddField("Psynergy", s.ToString());

                    await msg.ModifyAsync(m => { m.Content = ""; m.Embed = embed.Build(); });
                    await msg.AddReactionsAsync(emotes.ToArray());
                    i++;
                }
            }

            internal async Task GameOver()
            {
                var winners = Battle.GetTeam(Battle.GetWinner());
                if (winners.First() is PlayerFighter)
                {
                    winsInARow++;
                    winners.ConvertAll(s => (PlayerFighter)s).ForEach(async p => await ServerGames.UserWonBattle(p.avatar, winsInARow, LureCaps, p.battleStats, diff, textChannel));
                    if (!isEndless)
                    {
                        _ = WriteGameOver();
                    }
                    else
                    {
                        Battle.TeamA.ForEach(p =>
                        {
                            p.PPrecovery = Math.Min(8, p.PPrecovery + (winsInARow % 3 == 0 ? 1 : 0));
                            p.RemoveNearlyAllConditions();
                            p.Buffs = new List<Buff>();
                            p.Heal((uint)(p.stats.HP * 5 / 100));
                        });

                        var text = $"{winners.First().name}'s Party wins Battle {winsInARow}! Battle will reset shortly";
                        await Task.Delay(2000);
                        await StatusMsg.ModifyAsync(m => { m.Content = text; m.Embed = null; });

                        await Task.Delay(2000);

                        diff = (BattleDifficulty)Math.Min(4, 1 + winsInARow / stageLength);
                        SetRandomEnemies(ColossoBattle.Team.B);
                        Battle.Start();
                        _ = WriteBattleInit();
                    }
                }
                else
                {
                    if (isEndless)
                    {
                        diff = BattleDifficulty.Easy;
                    }

                    var losers = winners.First().battle.GetTeam(winners.First().enemies);
                    losers.ConvertAll(s => (PlayerFighter)s).ForEach(async p => await ServerGames.UserLostBattle(p.avatar, diff, textChannel));
                    _ = WriteGameOver();
                }
            }

            private async Task WriteGameOver()
            {
                await Task.Delay(2000);
                var winners = Battle.GetTeam(Battle.GetWinner());
                var text = $"{winners.First().name}'s Party wins! Battle will reset shortly";
                await StatusMsg.ModifyAsync(m => { m.Content = text; m.Embed = null; });
                await Task.Delay(2000);
                await Reset();
            }

            internal void SetEnemy(string enemy)
            {
                Battle.TeamB = new List<ColossoFighter>();
                EnemiesDatabase.GetEnemies(diff, enemy).ForEach(f => Battle.AddPlayer(f, ColossoBattle.Team.B));
                Console.WriteLine($"Up against {Battle.TeamB.First().name}");
            }
        }
    }
}