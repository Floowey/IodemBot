using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;
using IodemBot.Core.UserManagement;
using Newtonsoft.Json;

namespace IodemBot.ColossoBattles
{
    public abstract class BattleEnvironment : IDisposable
    {
        protected static readonly string[] NumberEmotes =
        {
            "0️⃣", "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣",
            "6️⃣", "7️⃣", "8️⃣", "9️⃣"
        };

        protected readonly List<SocketReaction> Reactions = new();
        protected Timer AutoTurn;

        protected Timer ResetIfNotActive;

        public BattleEnvironment(ColossoBattleService battleService, string name = null,
            ITextChannel lobbyChannel = null, bool isPersistent = true)
        {
            this.Name = name ?? "No Name";
            this.LobbyChannel = lobbyChannel;
            this.IsPersistent = isPersistent;
            this.BattleService = battleService;
            Global.Client.ReactionAdded += ReactionAdded;
        }

        public string Name { get; set; }
        public uint PlayersToStart { get; protected set; } = 4;
        protected ITextChannel LobbyChannel { get; set; }

        public ColossoBattle Battle { get; protected set; }
        public bool IsProcessing { get; private set; }
        public bool IsActive => Battle.IsActive;
        public bool IsPersistent { get; set; } = true;

        private ColossoBattleService BattleService { get; }
        internal abstract ulong[] ChannelIds { get; }

        public virtual void Dispose()
        {
            BattleService.RemoveBattleEnvironment(this);
            Global.Client.ReactionAdded -= ReactionAdded;
        }

        public PlayerFighter GetPlayer(ulong playerId)
        {
            return Battle.TeamA.Concat(Battle.TeamB).OfType<PlayerFighter>().FirstOrDefault(p => p.Id == playerId);
        }

        public abstract Task AddPlayer(PlayerFighter player, Team team = Team.A);

        public abstract Task AddPlayer(UserAccount user, Team team = Team.A);

        public abstract Task<(bool Success, string Message)> CanPlayerJoin(UserAccount user, Team team = Team.A);

        public abstract bool IsUsersMessage(PlayerFighter player, IUserMessage message);

        public async Task ReactionAdded(Cacheable<IUserMessage, ulong> message,
            Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            try
            {
                _ = ProcessReaction(message.Value, channel.Value, reaction);
            }
            catch (Exception e)
            {
                Console.Write("Reaction Error: " + e);
                File.WriteAllText("Logs/ReactionError_" + Global.DateString + ".txt", e.ToString());
            }

            await Task.CompletedTask;
        }

        protected abstract Task ProcessReaction(IUserMessage message, IMessageChannel channel, SocketReaction reaction);

        public bool ContainsPlayer(ulong userId)
        {
            return Battle.TeamA.OfType<PlayerFighter>().Any(p => p.Id == userId) ||
                   Battle.TeamB.OfType<PlayerFighter>().Any(p => p.Id == userId);
        }

        public async Task ProcessTurn(bool forced)
        {
            if (IsProcessing)
            {
                Console.WriteLine("Battle is still processing");
                return;
            }

            IsProcessing = true;
            var turnProcessed = false;
            try
            {
                turnProcessed = forced ? Battle.ForceTurn() : Battle.Turn();
            }
            catch (Exception e)
            {
                Console.Write("Turn did not Process correctly: " + e);
                File.WriteAllText("Logs/TurnError_" + Global.DateString + ".txt", e.ToString());
            }

            if (turnProcessed) await WriteField();
            IsProcessing = false;
        }

        protected async Task WriteField()
        {
            try
            {
                AutoTurn.Stop();
                await WriteBattle();
                if (Battle.IsActive)
                    AutoTurn.Start();
                else
                    await GameOver();
            }
            catch (Exception e)
            {
                Console.WriteLine("Battle did not draw correctly:" + e);
                File.WriteAllText("Logs/DrawError_" + Global.DateString + ".txt", e.ToString());
                //await WriteField();
            }
        }

        protected abstract Task WriteBattle();

        protected abstract Task WriteBattleInit();

        protected abstract Task GameOver();

        public abstract Task StartBattle();

        public abstract Task Reset(string msg = "");

        internal string GetStatus()
        {
            List<string> s = new();
            List<string> report = new();
            s.Add($"Battle is {(Battle.IsActive ? "" : "not")} active.");
            s.Add("\nTeam A:");
            Battle.TeamA.ForEach(p =>
            {
                s.Add(p.Name);
                s.Add($"{p.Stats.HP} / {p.Stats.MaxHP}HP");
                s.Add(
                    $"{(p.HasSelected ? $"Selected {p.SelectedMove.Name} at {p.SelectedMove.TargetNr}" : "Not Selected")}");
                s.Add("");
            });
            s.Add("\nTeam B:");
            Battle.TeamB.ForEach(p =>
            {
                s.Add(p.Name);
                s.Add($"{p.Stats.HP} / {p.Stats.MaxHP}HP");
                s.Add(
                    $"{(p.HasSelected ? $"Selected {p.SelectedMove.Name} at {p.SelectedMove.TargetNr}" : "Not Selected")}");
                s.Add("");
            });
            var battleReport = JsonConvert.SerializeObject(Battle, Formatting.Indented).Replace("{", "")
                .Replace("}", "").Replace("\"", "");
            Console.WriteLine(battleReport);
            File.WriteAllText($"Logs/Reports/Report_{Name}_{DateTime.Now:MM_dd_hh_mm}.log", battleReport);
            return string.Join("\n", s);
        }
    }
}