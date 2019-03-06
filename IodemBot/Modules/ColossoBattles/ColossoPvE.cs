using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using IodemBot.Core.Leveling;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;
using Newtonsoft.Json;

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
            battles = new List<BattleCollector>();
        }

        [Command("setup")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task SetupColosso()
        {
            textChannel = (SocketTextChannel)Global.Client.GetChannel(546760009741107216);
            battles = new List<BattleCollector>();
            Context.Message.DeleteAsync();
            var b = await GetBattleCollector(Context, "Bronze", BattleDifficulty.Easy);
            battles.Add(b);
            b = await GetBattleCollector(Context, "Silver", BattleDifficulty.Medium);
            battles.Add(b);
            b = await GetBattleCollector(Context, "Gold", BattleDifficulty.Hard);
            battles.Add(b);
        }

        private async Task<BattleCollector> GetBattleCollector(SocketCommandContext Context, string Name, BattleDifficulty diff)
        {
            var b = new BattleCollector();
            b.Name = Name;
            b.diff = diff;
            var channel = await Context.Guild.GetOrCreateTextChannelAsync("colosso-" + b.Name);
            await channel.ModifyAsync(c =>
            {
                c.CategoryId = ((ITextChannel)Context.Channel).CategoryId;
                c.Position = ((ITextChannel)Context.Channel).Position + 1;
            });
            await channel.SyncPermissionsAsync();
            var messages = await channel.GetMessagesAsync(100).FlattenAsync();
            await channel.DeleteMessagesAsync(messages);
            b.battleChannel = channel;
            b.enemyMsg = await b.battleChannel.SendMessageAsync($"Welcome to {b.Name} Battle!");
            b.lobbyMsg = await Context.Channel.SendMessageAsync($"{b.Name}: React with <:Fight:536919792813211648> to join the {b.Name} Battle and press <:Battle:536954571256365096> when you are ready to battle!");
            await b.reset();
            return b;
        }

        internal static async Task TryAddPlayer(SocketReaction reaction)
        {
            var battleCol = battles.Where(s => s.lobbyMsg.Id == reaction.MessageId).FirstOrDefault();
            if (battleCol == null) return;
            if (battleCol.battle.isActive) return;

            if (battleCol.messages.Values.Where(v => v is PlayerFighter).Where(s => ((PlayerFighter)s).avatar.ID == reaction.UserId).Any()) return;

            SocketGuildUser player = (SocketGuildUser)reaction.User.Value;
            var playerAvatar = UserAccounts.GetAccount(player);

            var p = new PlayerFighter(player);

            //var q = new PlayerFighter(player);

            battleCol.battle.AddPlayer(p, ColossoBattle.Team.A);

            //await reaction.Channel.SendMessageAsync($"{player.Username} wants to battle!");
            var playerMsg = await battleCol.battleChannel.SendMessageAsync($"{player.DisplayName()} wants to battle!");
            battleCol.messages.Add(playerMsg, p);

            if (battleCol.messages.Count == battleCol.playersToStart)
            {
                await TryStartBattle(reaction);
            }
        }

        internal static async Task ReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.User.Value.IsBot) return;

            if (reaction.Emote.Name == "Fight")
            {
                await TryAddPlayer(reaction);
            }
            else if (reaction.Emote.Name == "Battle")
            {
                await TryStartBattle(reaction);
            }

            var curBattle = battles.Where(b => b.battleChannel.Id == reaction.Channel.Id).FirstOrDefault();
            if (curBattle == null) return;
            var c = (RestUserMessage)await channel.GetMessageAsync(reaction.MessageId);
            curBattle.ProcessReaction(reaction, c);
        }

        private static async Task TryStartBattle(SocketReaction reaction)
        {
            //It's not necessary, but this should probably return a Task<bool>

            var battleCol = battles.Where(s => s.lobbyMsg.Id == reaction.MessageId).FirstOrDefault();
            if (battleCol.Equals(null)) return;
            if (battleCol.battle.isActive) return;

            if (battleCol.battle.sizeTeamA == 0) return;
            battleCol.battle.Start();
            await battleCol.WriteBattleInit();
        }

        public enum BattleDifficulty { Easy = 1, Medium = 2, Hard = 3 };

        internal class BattleCollector
        {
            internal ColossoBattle battle { get; set; }
            internal ITextChannel battleChannel { get; set; }
            internal RestUserMessage lobbyMsg { get; set; }
            internal IUserMessage enemyMsg { get; set; }
            internal IUserMessage statusMsg { get; set; }
            internal Dictionary<IUserMessage, ColossoFighter> messages { get; set; }
            internal uint playersToStart = 4;
            internal BattleDifficulty diff;
            internal string Name;
            internal Timer autoTurn;
            private List<SocketReaction> reactions = new List<SocketReaction>();

            internal async Task reset()
            {
                battle = new ColossoBattle();

                if (autoTurn != null) autoTurn.Dispose();

                if (messages != null) foreach (var k in messages.Keys) await k.DeleteAsync();
                messages = new Dictionary<IUserMessage, ColossoFighter>();

                await lobbyMsg.RemoveAllReactionsAsync();
                await lobbyMsg.AddReactionsAsync(new IEmote[]
                    {
                        Emote.Parse("<:Fight:536919792813211648>"),
                        Emote.Parse("<:Battle:536954571256365096>")
                    });

                if (enemyMsg != null)
                {
                    await enemyMsg.ModifyAsync(c => { c.Content = $"Welcome to {Name} Battle!"; c.Embed = null; });
                    await enemyMsg.RemoveAllReactionsAsync();
                }
                if (statusMsg != null)
                {
                    await statusMsg.DeleteAsync();
                    statusMsg = null;
                }
                EnemiesDatabase.getRandomEnemies(diff).ForEach(f => battle.AddPlayer(f, ColossoBattle.Team.B));

                autoTurn = new Timer()
                {
                    Interval = 30000,
                    AutoReset = false,
                    Enabled = false
                };
                autoTurn.Elapsed += OnTimerTicked;
                Console.WriteLine("Battle was reset.");
            }

            private async void OnTimerTicked(object sender, ElapsedEventArgs e)
            {
                await processTurn(forced: true);
            }

            internal async Task ProcessReaction(SocketReaction reaction, RestUserMessage c)
            {
                var tryMsg = messages.Keys.Where(k => k.Id == reaction.MessageId).FirstOrDefault();

                if(enemyMsg.Id == reaction.MessageId)
                {
                    tryMsg = statusMsg;
                }
                if (tryMsg == null)
                {
                    await c.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                    Console.WriteLine("No matching Message for user found.");
                    return;
                }

                if (!battle.isActive)
                {
                    await c.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                    Console.WriteLine("Battle not active.");
                    return;
                }
                var curPlayer = messages.Values.Where(p => p.name == ((SocketGuildUser)reaction.User.Value).DisplayName()).FirstOrDefault();
                var correctID = messages.Keys.Where(key => messages[key].name == curPlayer.name).First().Id;

                if (!numberEmotes.Contains(reaction.Emote.Name))
                {
                    if (reaction.MessageId != enemyMsg.Id && reaction.MessageId != correctID)
                    {
                        await c.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                        Console.WriteLine("Didn't click on own message.");
                        return;
                    }
                }

                if (!curPlayer.select(reaction.Emote.Name))
                {
                    await c.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                    Console.WriteLine("Couldn't select that move.");
                    return;
                }
                reactions.Add(reaction);
                await processTurn(forced: false);
            }

            internal async Task processTurn(bool forced)
            {
                bool turnProcessed = forced ? battle.ForceTurn() : battle.Turn();
                if (turnProcessed)
                {
                    await WriteBattle();
                    if (!battle.isActive)
                    {
                        await GameOver();
                    }
                };
            }

            internal async Task WriteBattle()
            {
                autoTurn.Stop();
                await WriteStatus();
                await WriteEnemies();
                await WritePlayers();
                autoTurn.Start();
            }

            private async Task WriteStatus()
            {
                if (battle.log.Count > 0)
                {
                    if (statusMsg == null)
                    {
                        statusMsg = await battleChannel.SendMessageAsync(battle.log.Aggregate("", (s, l) => s += l + "\n"));
                    }
                    else
                    {
                        await statusMsg.ModifyAsync(c => c.Content = battle.log.Aggregate("", (s, l) => s += l + "\n"));
                    }
                }
                else
                {
                    if (statusMsg == null)
                    {
                        string msg = messages.Values
                            .Where(p => p is PlayerFighter)
                            .Aggregate("", (s, v) => s += $"<@{((PlayerFighter)v).avatar.ID}>, ");
                        statusMsg = await battleChannel.SendMessageAsync($"{msg}get in position!");
                    }
                }
                
            }

            private async Task WriteEnemies()
            {
                var e = new EmbedBuilder();
                e.WithThumbnailUrl(battle.getTeam(ColossoBattle.Team.B).FirstOrDefault().imgUrl);
                var msg = enemyMsg;
                var i = 1;
                foreach (ColossoFighter fighter in battle.getTeam(ColossoBattle.Team.B))
                {
                    //e.AddField(numberEmotes[i], $"{fighter.name} {fighter.stats.HP}/{fighter.stats.maxHP}", true);
                    e.AddField($"{numberEmotes[i]} {fighter.ConditionsToString()}", $"{fighter.name}", true);
                    i++;
                }
                await msg.ModifyAsync(m => m.Embed = e.Build());
                var validReactions = reactions.Where(r => r.MessageId == enemyMsg.Id).ToList();
                foreach (var r in validReactions)
                {
                    await enemyMsg.RemoveReactionAsync(r.Emote, r.User.Value);
                    reactions.Remove(r);
                }
            }

            private async Task WritePlayers()
            {
                int i = 1;
                foreach (KeyValuePair<IUserMessage, ColossoFighter> k in messages)
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
                    //e.AddField();
                    embed.AddField($"{numberEmotes[i]} {fighter.ConditionsToString()}", fighter.name);
                    embed.AddField("HP", $"{fighter.stats.HP} / {fighter.stats.maxHP}", true);
                    embed.AddField("PP", $"{fighter.stats.PP} / {fighter.stats.maxPP}", true);
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
                e.WithThumbnailUrl(battle.getTeam(ColossoBattle.Team.B).FirstOrDefault().imgUrl);
                var msg = enemyMsg;
                var i = 1;
                foreach (ColossoFighter fighter in battle.getTeam(ColossoBattle.Team.B))
                {
                    //e.AddField(numberEmotes[i], $"{fighter.name} {fighter.stats.HP}/{fighter.stats.maxHP}", true);
                    e.AddField($"{numberEmotes[i]} {fighter.ConditionsToString()}", $"{fighter.name}", true);
                    i++;
                }
                await msg.ModifyAsync(m => m.Embed = e.Build());
                //var countA = battle.getTeam(ColossoBattle.Team.A).Count;
                var countA = 1;
                var countB = battle.getTeam(ColossoBattle.Team.B).Count;
                if (countA > 1 || countB > 1){
                    for (int j = 1; j <= Math.Max(countA, countB); j++)
                    {
                        await msg.AddReactionAsync(new Emoji(numberEmotes[j]));
                    }
                }
            }

            private async Task WritePlayersInit()
            {
                int i = 1;
                foreach (KeyValuePair<IUserMessage, ColossoFighter> k in messages)
                {
                    var msg = k.Key;
                    var embed = new EmbedBuilder();
                    var fighter = k.Value;
                    //e.WithAuthor($"{numberEmotes[i]} {fighter.name}");
                    embed.WithThumbnailUrl(fighter.imgUrl);
                    //e.AddField();
                    embed.AddField($"{numberEmotes[i]} {fighter.ConditionsToString()}", fighter.name);
                    embed.AddField("HP", $"{fighter.stats.HP} / {fighter.stats.maxHP}", true);
                    embed.AddField("PP", $"{fighter.stats.PP} / {fighter.stats.maxPP}", true);
                    var s = new StringBuilder();
                    List<IEmote> emotes = new List<IEmote>();
                    if(messages.Count > 1)
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
                autoTurn.Stop();
                autoTurn.Dispose();
                var winners = battle.getTeam(battle.getWinner());
                var text = $"{winners.First().name}'s Party wins! Battle will reset shortly";
                await statusMsg.ModifyAsync(m => { m.Content = text; m.Embed = null; });
                if (winners.First() is PlayerFighter)
                {
                    winners.ConvertAll(s => (PlayerFighter)s).ForEach(async p => await ServerGames.UserWonBattle(p.avatar, diff, textChannel));
                }
                else
                {
                    var losers = winners.First().battle.getTeam(winners.First().enemies);
                    losers.ConvertAll(s => (PlayerFighter)s).ForEach(async p => await ServerGames.UserLostBattle(p.avatar, diff, textChannel));
                }
                await Task.Delay(5000);
                await reset();
            }

            
        }
    }
}
