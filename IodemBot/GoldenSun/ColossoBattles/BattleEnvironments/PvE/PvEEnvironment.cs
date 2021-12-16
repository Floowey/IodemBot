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
        private static readonly string[] TutorialTips =
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

        private bool _wasReset;

        public ITextChannel BattleChannel;
        protected IUserMessage EnemyMessage;
        protected Dictionary<IUserMessage, PlayerFighter> PlayerMessages = new();
        protected IUserMessage StatusMessage;
        protected IUserMessage SummonsMessage;
        protected bool WasJustReset = true;

        protected PvEEnvironment(ColossoBattleService battleService, string name, ITextChannel lobbyChannel,
            bool isPersistent, ITextChannel battleChannel)
            : base(battleService, name, lobbyChannel, isPersistent)
        {
            BattleChannel = battleChannel;
            LobbyChannel = lobbyChannel;
        }

        public PlayerFighterFactory Factory { get; set; } = new();

        internal override ulong[] ChannelIds => new[] { BattleChannel.Id };

        private async Task Initialize()
        {
            EnemyMessage = await BattleChannel.SendMessageAsync(GetEnemyMessageString(),
                component: ControlBattleComponents.GetControlComponent());
            SummonsMessage = await BattleChannel.SendMessageAsync("Good Luck!");
        }

        public override void Dispose()
        {
            base.Dispose();
            AutoTurn?.Dispose();
            ResetIfNotActive?.Dispose();
            if (!IsPersistent) _ = BattleChannel.DeleteAsync();
        }

        protected virtual string GetEnemyMessageString()
        {
            return $"Welcome to {Name} Battle!";
        }

        protected virtual string GetStartBattleString()
        {
            var msg = string.Join(", ", PlayerMessages.Select(v => $"<@{v.Value.Id}>"));
            return $"{msg} get into Position!";
        }

        protected string GetWinMessageString()
        {
            var winners = Battle.GetTeam(Battle.GetWinner());
            return $"{winners.FirstOrDefault().Name}'s party wins! Battle will reset shortly.";
        }

        public abstract void SetEnemy(string enemy);

        public abstract void SetNextEnemy();

        protected override async Task ProcessReaction(IUserMessage message, IMessageChannel channel,
            SocketReaction reaction)
        {
            try
            {
                if (reaction.User.Value.IsBot)
                    return;
                if (channel.Id != BattleChannel.Id) return;

                var diffEmotesStrings = new[]
                    {"<:Bronze:537214232203100190>", "<:Silver:537214282891395072>", "<:Gold:537214319591555073>"};
                var diffEmotes = diffEmotesStrings.Select(Emote.Parse);
                if (diffEmotes.Contains(reaction.Emote) && this is SingleBattleEnvironment environment)
                {
                    var diff = new Dictionary<string, BattleDifficulty>
                    {
                        {"Bronze", BattleDifficulty.Easy},
                        {"Silver", BattleDifficulty.Medium},
                        {"Gold", BattleDifficulty.Hard}
                    };
                    environment.InternalDiff = diff[reaction.Emote.Name];
                    await Reset("Difficulty changed");
                    return;
                }

                IUserMessage c = null;
                if ((StatusMessage?.Id ?? 0) == reaction.MessageId) c = StatusMessage;
                if ((EnemyMessage?.Id ?? 0) == reaction.MessageId) c = EnemyMessage;
                if ((SummonsMessage?.Id ?? 0) == reaction.MessageId) c = SummonsMessage;
                if (PlayerMessages.Keys.Any(k => k.Id == reaction.MessageId))
                    c = PlayerMessages.Keys.FirstOrDefault(k => k.Id == reaction.MessageId);

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
            }
            catch (Exception e)
            {
                Console.WriteLine($"Colosso Turn Processing Error {reaction.Emote}: " + e);
                File.WriteAllText($"Logs/Crashes/Error_{DateTime.Now.Date}.log", e.ToString());
            }
        }

        protected async Task RedrawBattle()
        {
            AutoTurn.Stop();
            WasJustReset = false;

            await WriteBattleInit();
            AutoTurn.Start();
        }

        public override async Task AddPlayer(UserAccount user, Team team = Team.A)
        {
            var p = Factory.CreatePlayerFighter(user);
            await AddPlayer(p);
        }

        public override async Task AddPlayer(PlayerFighter player, Team team = Team.A)
        {
            if (Battle.IsActive) return;

            Battle.AddPlayer(player, Team.A);

            ResetIfNotActive.Stop();
            ResetIfNotActive.Start();

            var playerMsg = await BattleChannel.SendMessageAsync($"{player.Name} wants to battle!");
            PlayerMessages.Add(playerMsg, player);

            if (PlayerMessages.Count == PlayersToStart) _ = StartBattle();
        }

        public override Task<(bool Success, string Message)> CanPlayerJoin(UserAccount user, Team team = Team.A)
        {
            if (GetPlayer(user.Id) != null)
                return Task.FromResult((false, "You are already in this battle."));

            if (Battle.GetTeam(team).Count >= PlayersToStart)
                return Task.FromResult((false, "This team is already full."));

            return Task.FromResult((true, (string)null));
        }

        public override async Task Reset(string msg = "")
        {
            Battle = new ColossoBattle();
            PlayerMessages
                .Values
                .SelectMany(u => u.Moves.OfType<Djinn>())
                .ToList()
                .ForEach(d => d.Reset());

            foreach (var k in PlayerMessages.Keys) _ = k.DeleteAsync();
            Factory.Djinn.Clear();
            Factory.Summons.Clear();
            PlayerMessages.Clear();

            if (!IsPersistent && _wasReset)
            {
                Console.WriteLine($"{Name} was disposed: {msg}");
                Dispose();
                return;
            }

            if (EnemyMessage == null)
            {
                await Initialize();
            }
            else
            {
                await EnemyMessage.ModifyAsync(c =>
                {
                    c.Content = GetEnemyMessageString();
                    c.Embed = null;
                    c.Components = ControlBattleComponents.GetControlComponent();
                });
                WasJustReset = true;
            }

            if (SummonsMessage == null)
                await Initialize();
            else
                _ = SummonsMessage.ModifyAsync(m =>
                {
                    m.Content = $"Good Luck!\n{TutorialTips.Random()}";
                    m.Embed = null;
                    m.Components = null;
                });

            if (StatusMessage != null)
            {
                _ = StatusMessage.DeleteAsync();
                StatusMessage = null;
            }

            SetNextEnemy();

            if (AutoTurn != null) AutoTurn.Dispose();
            if (ResetIfNotActive != null) ResetIfNotActive.Dispose();
            AutoTurn = new Timer
            {
                Interval = 45000,
                AutoReset = false,
                Enabled = false
            };
            AutoTurn.Elapsed += TurnTimeElapsed;
            ResetIfNotActive = new Timer
            {
                Interval = 120000,
                AutoReset = false,
                Enabled = !IsPersistent
            };
            ResetIfNotActive.Elapsed += BattleWasNotStartetInTime;

            _wasReset = true;
            Console.WriteLine($"{Name} was reset: {msg}");
        }

        private async void BattleWasNotStartetInTime(object sender, ElapsedEventArgs e)
        {
            if (!IsActive)
                _ = Reset("Not started in time");
            else
                Console.WriteLine("Battle tried to reset while active. Timer Status: " + ResetIfNotActive.Enabled);
            await Task.CompletedTask;
        }

        private async void TurnTimeElapsed(object sender, ElapsedEventArgs e)
        {
            _ = ProcessTurn(true);
            await Task.CompletedTask;
        }

        public override async Task StartBattle()
        {
            if (Battle.IsActive) return;

            if (Battle.SizeTeamA == 0) return;

            if (!PlayerMessages.Values.Any(p => p.Moves.Any(m => m is Summon)))
                PlayerMessages.Values.ToList().ForEach(p => p.Moves.AddRange(Factory.PossibleSummons));

            ResetIfNotActive.Stop();
            Battle.Start();
            await WriteBattleInit();
            AutoTurn.Start();
            WasJustReset = false;
        }

        protected override async Task WriteBattle()
        {
            var delay = Global.Client.Latency / 2;
            try
            {
                //await Task.Delay(delay);
                await WriteStatus();
                //await Task.Delay(delay);
                await WriteSummons();
                //await Task.Delay(delay);
                await WriteEnemies();
                //await Task.Delay(delay);
                await WritePlayers();
                //await Task.Delay(delay);
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
                var msg = await BattleChannel.SendMessageAsync("Timed out while drawing Battle, retrying in 30s.");
                await Task.Delay(30000);
                await msg.DeleteAsync();
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
                //await Task.Delay(delay);
                await WriteSummonsInit();
                //await Task.Delay(delay);
                await WriteEnemiesInit();
                //await Task.Delay(delay);
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
                await Task.Delay(30000);
                await WriteBattleInit();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception while writing Battle:" + e);
                throw new Exception("Exception while writing Battle", e);
            }
        }

        protected virtual async Task WriteEnemiesInit()
        {
            await EnemyMessage.RemoveAllReactionsAsync();
            await WriteEnemies();
        }

        protected virtual async Task WriteEnemies()
        {
            var e = GetEnemyEmbedBuilder();
            if (EnemyMessage.Embeds.Count == 0 ||
                !EnemyMessage.Embeds.FirstOrDefault().ToEmbedBuilder().AllFieldsEqual(e))
                await EnemyMessage.ModifyAsync(m =>
                {
                    m.Content = "";
                    m.Embed = e.Build();
                    m.Components = null;
                });
        }

        protected virtual EmbedBuilder GetEnemyEmbedBuilder()
        {
            var e = new EmbedBuilder();
            if (Battle.SizeTeamB > 0) e.WithThumbnailUrl(Battle.GetTeam(Team.B).FirstOrDefault().ImgUrl);
            var i = 1;
            foreach (var fighter in Battle.GetTeam(Team.B))
            {
                var desc = $"\u200B{fighter.ConditionsToString()}".Trim();
                e.AddField($"{fighter.Name}", desc.IsNullOrEmpty() ? "\u200B" : desc, true);
                //e.AddField($"{fighter.Name}", $"{fighter.ConditionsToString()}".Trim(), true);
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
            if (embed != null && (SummonsMessage.Embeds.Count == 0 ||
                                  !SummonsMessage.Embeds.FirstOrDefault().ToEmbedBuilder().AllFieldsEqual(embed)))
                _ = SummonsMessage.ModifyAsync(m =>
                {
                    m.Embed = embed.Build();
                    m.Components =
                        ControlBattleComponents.GetSummonsComponent((PlayerFighter)Battle.GetTeam(Team.A).First());
                });
            await Task.CompletedTask;
        }

        protected virtual EmbedBuilder GetDjinnEmbedBuilder()
        {
            var allDjinn = PlayerMessages.Values.SelectMany(p => p.Moves.OfType<Djinn>()).ToList();
            var standbyDjinn = allDjinn.Where(d => d.State == DjinnState.Standby);
            var recoveryDjinn = allDjinn.Where(d => d.State == DjinnState.Recovery);
            if (allDjinn.Count == 0) return null;
            var embed = new EmbedBuilder();
            //.WithThumbnailUrl("https://cdn.discordapp.com/attachments/497696510688100352/640300243820216336/unknown.png");

            var allEls = new[] { Element.Venus, Element.Mars, Element.Jupiter, Element.Mercury };
            var necessaryFields = allEls.Count(el => allDjinn.OfElement(el).Any());

            foreach (var el in allEls)
                if (allDjinn.OfElement(el).Any())
                {
                    var standby = string.Join(" ", standbyDjinn.OfElement(el).Select(d => d.Emote));
                    var recovery = string.Join(" ", recoveryDjinn.OfElement(el).Select(d => d.Emote));
                    embed.WithColor(Colors.Get(standbyDjinn.Select(e => e.Element.ToString()).ToList()));

                    var djinnField = $"\u200B{standby}{(recovery.IsNullOrEmpty() ? "" : $"\u200B({recovery})")}";

                    //embed.AddField(Emotes.GetIcon(el), ($"{standby}" +
                    //    $"{(!standby.IsNullOrEmpty() && !recovery.IsNullOrEmpty() ? "\n" : "\u200b")}" +
                    //    $"{(recovery.IsNullOrEmpty() ? "" : $"({recovery})")}").Trim(), true);
                    embed.AddField($"\u200B{Emotes.GetIcon(el)}", djinnField.IsNullOrEmpty() ? "\u200b" : djinnField, true);
                    if (necessaryFields > 2 && embed.Fields.Count == 2 || embed.Fields.Count == 5)
                        embed.AddField("\u200b", "\u200b", true);
                }

            return embed;
        }

        protected virtual async Task WritePlayers()
        {
            var tasks = new List<Task>();
            foreach (var k in PlayerMessages)
            {
                var msg = k.Key;
                var embed = new EmbedBuilder();
                var fighter = k.Value;

                embed.WithThumbnailUrl(fighter.ImgUrl);
                embed.WithColor(
                    Colors.Get(fighter.Moves.OfType<Psynergy>().Select(p => p.Element.ToString()).ToArray()));
                //embed.AddField($"{fighter.Name}{fighter.ConditionsToString()}",
                //    $"**HP**: {fighter.Stats.HP} / {fighter.Stats.MaxHP}\n**PP**: {fighter.Stats.PP} / {fighter.Stats.MaxPP}");
                embed.AddField($"{fighter.Name}{fighter.ConditionsToString()}",
                    $"{Utilities.GetProgressBar(fighter.Stats.HP * 100 / fighter.Stats.MaxHP)} **HP {fighter.Stats.HP}**\n" +
                    $"{Utilities.GetProgressBar(fighter.Stats.PP * 100 / Math.Max(1, fighter.Stats.MaxPP))} **PP {fighter.Stats.PP}**"
                );

                tasks.Add(msg.ModifyAsync(m =>
                {
                    m.Content = "";
                    m.Embed = embed.Build();
                    m.Components = ControlBattleComponents.GetPlayerControlComponents(fighter);
                }));

                if (fighter.AutoTurnsInARow >= 2)
                {
                    var ping = await msg.Channel.SendMessageAsync($"<@{fighter.Id}>");
                    _ = ping.DeleteAsync();
                }
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
            if (Battle.Log.Count > 0 && Battle.TurnNumber > 0)
            {
                if (StatusMessage == null)
                    StatusMessage =
                        await BattleChannel.SendMessageAsync(Battle.Log.Aggregate("", (s, l) => s += l + "\n"));
                else
                    await StatusMessage.ModifyAsync(c => c.Content = Battle.Log.Aggregate("", (s, l) => s += l + "\n"));
            }
            else
            {
                StatusMessage ??= await BattleChannel.SendMessageAsync(GetStartBattleString());
            }
        }

        protected virtual async Task WriteGameOver()
        {
            await Task.Delay(3000);
            var text = GetWinMessageString();
            await StatusMessage.ModifyAsync(m =>
            {
                m.Content = text;
                m.Embed = null;
            });
            await Task.Delay(2000);
            await Reset("Game Over");
        }

        public override bool IsUsersMessage(PlayerFighter user, IUserMessage message)
        {
            return SummonsMessage.Id == message.Id ||
                   PlayerMessages.Any(m => m.Key.Id == message.Id && m.Value.Id == user.Id);
        }
    }
}