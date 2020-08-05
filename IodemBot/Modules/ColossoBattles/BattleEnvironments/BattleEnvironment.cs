﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace IodemBot.Modules.ColossoBattles
{
    public abstract class BattleEnvironment : IDisposable
    {
        protected static string[] numberEmotes = new string[] { "0️⃣", "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣",
            "6️⃣", "7️⃣", "8️⃣", "9️⃣" };

        public string Name { get; private set; }
        protected uint PlayersToStart { get; set; } = 4;

        protected ColossoBattle Battle;
        protected Timer autoTurn;

        protected Timer resetIfNotActive;
        protected ITextChannel lobbyChannel;
        protected readonly List<SocketReaction> reactions = new List<SocketReaction>();
        private bool isProcessing = false;
        public bool IsActive { get { return Battle.SizeTeamA > 0; } }

        internal abstract ulong[] GetIds { get; }
        public bool IsPersistent { get; set; } = true;

        public BattleEnvironment(string Name, ITextChannel lobbyChannel, bool isPersistent)
        {
            this.Name = Name;
            this.lobbyChannel = lobbyChannel;
            this.IsPersistent = isPersistent;
            Global.Client.ReactionAdded += ProcessReaction;
        }

        protected abstract Task ProcessReaction(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel message, SocketReaction reaction);

        public bool ContainsPlayer(ulong UserId)
        {
            return Battle.TeamA.OfType<PlayerFighter>().Any(p => p.guildUser.Id == UserId) || Battle.TeamB.OfType<PlayerFighter>().Any(p => p.guildUser.Id == UserId);
        }

        public async Task ProcessTurn(bool forced)
        {
            if (isProcessing)
            {
                Console.WriteLine("Battle is still processing");
                return;
            }
            isProcessing = true;
            bool turnProcessed = false;
            try
            {
                turnProcessed = forced ? Battle.ForceTurn() : Battle.Turn();
            }
            catch (Exception e)
            {
                Console.Write("Turn did not Process correctly: " + e.ToString());
                File.WriteAllText("Logs/TurnError_" + Global.DateString + ".txt", e.ToString());
            }

            if (turnProcessed)
            {
                await WriteField();
            };
            isProcessing = false;
        }

        protected async Task WriteField()
        {
            try
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
            }
            catch (Exception e)
            {
                Console.WriteLine("Battle did not draw correctly:" + e.ToString());
                File.WriteAllText("Logs/DrawError_" + Global.DateString + ".txt", e.ToString());
                //await WriteField();
            }
        }

        protected abstract Task WriteBattle();

        protected abstract Task WriteBattleInit();

        protected abstract Task GameOver();

        protected abstract Task StartBattle();

        public abstract Task Reset();

        public virtual void Dispose()
        {
            Global.Client.ReactionAdded -= ProcessReaction;
        }

        internal string GetStatus()
        {
#pragma warning disable IDE0028 // Simplify collection initialization
            List<string> s = new List<string>();
            List<string> report = new List<string>();
#pragma warning restore IDE0028 // Simplify collection initialization
            s.Add($"Battle is {(Battle.isActive ? "" : "not")} active.");
            s.Add($"\nTeam A:");
            Battle.TeamA.ForEach(p =>
            {
                s.Add(p.Name);
                s.Add($"{p.Stats.HP} / {p.Stats.MaxHP}HP");
                s.Add($"{(p.hasSelected ? $"Selected {p.selected.Name} at {p.selected.TargetNr}" : "Not Selected")}");
                s.Add("");
            });
            s.Add($"\nTeam B:");
            Battle.TeamB.ForEach(p =>
            {
                s.Add(p.Name);
                s.Add($"{p.Stats.HP} / {p.Stats.MaxHP}HP");
                s.Add($"{(p.hasSelected ? $"Selected {p.selected.Name} at {p.selected.TargetNr}" : "Not Selected")}");
                s.Add("");
            });
            var BattleReport = JsonConvert.SerializeObject(Battle, Formatting.Indented).Replace("{", "").Replace("}", "").Replace("\"", "");
            Console.WriteLine(BattleReport);
            File.WriteAllText($"Logs/Reports/Report_{Name}_{DateTime.Now:MM_dd_hh_mm}.log", BattleReport);
            return string.Join("\n", s);
        }
    }
}