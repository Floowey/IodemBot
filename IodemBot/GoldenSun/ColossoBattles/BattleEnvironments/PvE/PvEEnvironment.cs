using System;
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
using IodemBot.Modules.BattleActions;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.ColossoBattles
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

        internal override ulong[] ChannelIds => new[] { BattleChannel.Id };

        public PvEEnvironment(string Name, ITextChannel lobbyChannel, bool isPersistent, ITextChannel BattleChannel) : base(Name, lobbyChannel, isPersistent)
        {
            this.BattleChannel = BattleChannel;
            this.lobbyChannel = lobbyChannel;
        }

        private async Task Initialize()
        {
            EnemyMessage = await BattleChannel.SendMessageAsync(GetEnemyMessageString(), component: ControlBattleComponents.GetControlComponent());
            SummonsMessage = await BattleChannel.SendMessageAsync("Good Luck!");
            return;
        }

        public override void Dispose()
        {
            base.Dispose();
            autoTurn?.Dispose();
            resetIfNotActive?.Dispose();
            if (!IsPersistent)
            {
                _ = BattleChannel.DeleteAsync();
            }
        }

        protected virtual string GetEnemyMessageString()
        {
            return $"Welcome to {Name} Battle!";
        }

        protected virtual string GetStartBattleString()
        {
            string msg = string.Join(", ", PlayerMessages.Select(v => $"<@{v.Value.Id}>"));
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

        protected override async Task ProcessReaction(IUserMessage message, IMessageChannel channel, SocketReaction reaction)
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

                var diffEmotesStrings = new[] { "<:Bronze:537214232203100190>", "<:Silver:537214282891395072>", "<:Gold:537214319591555073>" };
                var diffEmotes = diffEmotesStrings.Select(e => Emote.Parse(e));
                if (diffEmotes.Contains(reaction.Emote) && this is SingleBattleEnvironment environment)
                {
                    Dictionary<string, BattleDifficulty> diff = new Dictionary<string, BattleDifficulty>()
                    {
                        { "Bronze", BattleDifficulty.Easy },
                        { "Silver", BattleDifficulty.Medium },
                        { "Gold", BattleDifficulty.Hard }
                    };
                    environment.internalDiff = diff[reaction.Emote.Name];
                    await Reset($"Difficulty changed");
                    return;
                }

                IUserMessage c = null;
                if ((StatusMessage?.Id ?? 0) == reaction.MessageId)
                {
                    c = StatusMessage;
                }
                if ((EnemyMessage?.Id ?? 0) == reaction.MessageId)
                {
                    c = EnemyMessage;
                }
                if ((SummonsMessage?.Id ?? 0) == reaction.MessageId)
                {
                    c = SummonsMessage;
                }
                if (PlayerMessages.Keys.Any(k => k.Id == reaction.MessageId))
                {
                    c = PlayerMessages.Keys.Where(k => k.Id == reaction.MessageId).FirstOrDefault();
                }

                if (c == null)
                {
                    c = (IUserMessage)await channel.GetMessageAsync(reaction.MessageId);
                    _ = c.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                    Console.WriteLine("No matching Message for User found.");
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

                var curPlayer = PlayerMessages.Values.Where(p => p.Id == reaction.User.Value.Id).FirstOrDefault();
                if (curPlayer == null)
                {
                    _ = c.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                    Console.WriteLine("Player not in this room.");
                    return;
                }
                var correctID = PlayerMessages.Keys.Where(key => PlayerMessages[key].Id == curPlayer.Id).First().Id;

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
                Console.WriteLine($"Colosso Turn Processing Error {reaction.Emote}: " + e);
                File.WriteAllText($"Logs/Crashes/Error_{DateTime.Now.Date}.log", e.ToString());
            }
        }

        protected async Task RedrawBattle()
        {
            autoTurn.Stop();
            wasJustReset = false;
            
            await WriteBattleInit();
            autoTurn.Start();
        }

        protected virtual async Task AddPlayer(SocketReaction reaction)
        {
            if (PlayerMessages.Values.Any(s => (s.Id == reaction.UserId)))
            {
                return;
            }
            SocketGuildUser player = (SocketGuildUser)reaction.User.Value;
            var user = EntityConverter.ConvertUser(player);

            await AddPlayer(user);
        }
        public override async Task AddPlayer(UserAccount user, Team team = Team.A)
        { 
            var p = Factory.CreatePlayerFighter(user);
            await AddPlayer(p);
        }
        public override async Task AddPlayer(PlayerFighter player, Team team = Team.A)
        {
            if (Battle.isActive)
            {
                return;
            }

            Battle.AddPlayer(player, Team.A);

            resetIfNotActive.Stop();
            resetIfNotActive.Start();

            var playerMsg = await BattleChannel.SendMessageAsync($"{player.Name} wants to battle!");
            PlayerMessages.Add(playerMsg, player);

            if (PlayerMessages.Count == PlayersToStart)
            {
                _ = StartBattle();
            }
        }

        private static readonly string[] tutorialTips = new[]
        {
            "Djinn are your friends! Use them to make up for weaknesses in your class!",
            "You want to play with multiple setups? Save your loadout with `i!loadout save <My Loadout Name>` to access it later!",
            "Make sure to keep opening your daily chests! Every 5th item will be better than the ones before!",
            "Struggling to beat a dungeon? Keep on grinding, you might find good gear to help you progress!",
            "Some dungeons might feature riddles, take your time to solve them by reacting with :pause_button:",
            "i!train is a great way to earn some solo xp if running solo battles are a little too difficult.",
            "The @Colosso Guard are here to ensure that things run smoothly. If there are any issues, let them know. Note that they cannot fix delays or lag.",
            "Remember to keep commands locked away in <#358276942337671178> to ensure that the <#546760009741107216> channel doesn't get flooded with shenanigans.",
            "No djinn were harmed in the making of this code. ~~Except for Flint, but he had it coming.~~",
            "Take your time, especially if a lot of people are playing. Iodem can only do so much at once!",
            "Collecting Djinn of your class element can help you power up in more ways than one!",
            "There are a lot of classes in Golden Sun. Try them out! Each has strengths and weaknesses that may help you if you get stuck.",
            "Be sure to check the pins in #colosso-talks! A lot of good information is saved there.",
            "You have two equipment sets: warrior and mage. Items can be equipped to one or both sets!",
            "Want to stay up to date with how the bot progresses? Give you self the @Colosso Adept role with `i!giverole Colosso Adept`!",
            "If you want to support this projects, look no further than `i!credits`!"
        };
        public override async Task Reset(string msg = "")
        {
            Battle = new ColossoBattle();
            
            PlayerMessages
                .Values
                .SelectMany(u => u.Moves.OfType<Djinn>())
                .ToList()
                .ForEach(d => d.Reset());

            foreach (var k in PlayerMessages.Keys)
            {
                await k.DeleteAsync();
            }
            Factory.djinn.Clear();
            Factory.summons.Clear();
            PlayerMessages.Clear();

            if (!IsPersistent && WasReset)
            {
                Console.WriteLine($"{Name} was disposed: {msg}");
                Dispose(); return;
            }

            if (EnemyMessage == null)
            {
                await Initialize();
            }
            else
            {
                await EnemyMessage.ModifyAsync(c => {
                    c.Content = GetEnemyMessageString();
                    c.Embed = null;
                    c.Components = ControlBattleComponents.GetControlComponent();
                    });
                wasJustReset = true;
            }

            if (SummonsMessage == null)
            {
                await Initialize();
            }
            else
            {
                _ = SummonsMessage.ModifyAsync(m => { m.Content = $"Good Luck!\n{tutorialTips.Random()}"; m.Embed = null; m.Components = null; });    
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
                Enabled = !IsPersistent
            };
            resetIfNotActive.Elapsed += BattleWasNotStartetInTime;

            WasReset = true;
            Console.WriteLine($"{Name} was reset: {msg}");
        }

        private async void BattleWasNotStartetInTime(object sender, ElapsedEventArgs e)
        {
            if (!IsActive)
            {
                _ = Reset("Not started in time");
            } else
            {
                Console.WriteLine("Battle tried to reset while active. Timer Status: " + resetIfNotActive.Enabled);
            }
            await Task.CompletedTask;
        }

        private async void TurnTimeElapsed(object sender, ElapsedEventArgs e)
        {
            _ = ProcessTurn(forced: true);
            await Task.CompletedTask;
        }

        public override async Task StartBattle()
        {
            if (Battle.isActive)
            {
                return;
            }

            if (Battle.SizeTeamA == 0)
            {
                return;
            }

            if (!PlayerMessages.Values.Any(p => p.Moves.Any(m => m is Summon)))
            {
                PlayerMessages.Values.ToList().ForEach(p => p.Moves.AddRange(Factory.PossibleSummons));
            }

            resetIfNotActive.Stop();
            Battle.Start();
            await WriteBattleInit();
            autoTurn.Start();
            wasJustReset = false;
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
                Console.WriteLine("Failed drawing Battle, retrying." + e.ToString());
                Battle.log.Add("Failed drawing Battle, retrying.");
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
                Console.WriteLine("Timed out while drawing Battle, retrying." + e.ToString());
                Battle.log.Add("Timed out while drawing Battle, retrying in 30s.");
                var msg = await BattleChannel.SendMessageAsync("Timed out while drawing Battle, retrying in 30s.");
                await Task.Delay(300000);
                await msg.DeleteAsync();
                await WriteBattle();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception while writing Battle: " + e.ToString());
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
                Console.WriteLine("Failed drawing Battle, retrying: " + e.ToString());
                Battle.log.Add("Failed drawing Battle, retrying.");
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
                Console.WriteLine("Timed out while drawing Battle, retrying: " + e.ToString());
                Battle.log.Add("Timed out while drawing Battle, retrying in 30s");
                await Task.Delay(30000);
                await WriteBattleInit();
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

            await Task.WhenAll(tasks);
        }

        protected virtual async Task WriteEnemiesInit()
        {
            var tasks = new List<Task>
            {
                WriteEnemyEmbed()
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
                    m.Content = ""; m.Embed = e.Build(); m.Components = null;
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
                e.AddField($"{numberEmotes[i]} {fighter.ConditionsToString()}".Trim(), $"{fighter.Name}", true);
                i++;
            }
            return e;
        }

        protected virtual async Task WriteSummonsInit()
        { 
            await WriteSummons();
        }

        protected virtual async Task WriteSummons()
        {
            var tasks = new List<Task>();
            var embed = GetDjinnEmbedBuilder();
            if (embed != null && (SummonsMessage.Embeds.Count == 0 || !SummonsMessage.Embeds.FirstOrDefault().ToEmbedBuilder().AllFieldsEqual(embed)))
            {
                _ = SummonsMessage.ModifyAsync(m =>
                {
                    m.Embed = embed.Build();
                    m.Components = ControlBattleComponents.GetSummonsComponent((PlayerFighter)Battle.GetTeam(Team.A).First());

                });
            }
            await Task.CompletedTask;
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
            EmbedBuilder embed = new EmbedBuilder();
            //.WithThumbnailUrl("https://cdn.discordapp.com/attachments/497696510688100352/640300243820216336/unknown.png");

            foreach (var el in new[] { Element.Venus, Element.Mars, Element.Jupiter, Element.Mercury })
            {
                if (allDjinn.OfElement(el).Count() > 0)
                {
                    var standby = string.Join(" ", standbyDjinn.OfElement(el).Select(d => d.Emote));
                    var recovery = string.Join(" ", recoveryDjinn.OfElement(el).Select(d => d.Emote));
                    embed.WithColor(Colors.Get(standbyDjinn.Select(e => e.Element.ToString()).ToList()));

                    embed.AddField(Emotes.GetIcon(el), ($"{standby}" +
                        $"{(!standby.IsNullOrEmpty() && !recovery.IsNullOrEmpty() ? "\n" : "\u200b")}" +
                        $"{(recovery.IsNullOrEmpty() ? "" : $"({recovery})")}").Trim(), true);
                    if (embed.Fields.Count == 2 || embed.Fields.Count == 5)
                    {
                        embed.AddField("\u200b", "\u200b", true);
                    }
                }
            }

            return embed;
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
                //embed.AddField("Psynergy", string.Join(" | ", s));

                
                tasks.Add(msg.ModifyAsync(m => { 
                    m.Embed = embed.Build(); 
                    m.Components = ControlBattleComponents.GetPlayerControlComponents(fighter); 
                }));
                

                if (fighter.AutoTurnsInARow >= 2)
                {
                    var ping = await msg.Channel.SendMessageAsync($"<@{fighter.Id}>");
                    _ = ping.DeleteAsync();
                }
                i++;
            }
            await Task.WhenAll(tasks);
        }

        protected virtual async Task WritePlayersInit()
        {
            await WritePlayers();
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
            await Reset("Game Over");
        }

        public override bool IsUsersMessage(PlayerFighter user, IUserMessage message)
        {
            return SummonsMessage.Id == message.Id || PlayerMessages.Any(m => m.Key.Id == message.Id && m.Value.Id == user.Id);
        }
    }
}
