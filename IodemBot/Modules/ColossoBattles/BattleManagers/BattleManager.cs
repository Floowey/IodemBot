using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

namespace IodemBot.Modules.ColossoBattles
{
    public abstract class BattleManager : IDisposable
    {
        protected static string[] numberEmotes = new string[] { "\u0030\u20E3", "\u0031\u20E3", "\u0032\u20E3", "\u0033\u20E3", "\u0034\u20E3", "\u0035\u20E3",
            "\u0036\u20E3", "\u0037\u20E3", "\u0038\u20E3", "\u0039\u20E3" };

        public string Name { get; private set; }
        protected uint PlayersToStart { get; set; } = 4;
        protected ColossoBattle Battle;
        protected Timer autoTurn;
        protected Timer resetIfNotActive;
        protected ITextChannel lobbyChannel;
        protected readonly List<SocketReaction> reactions = new List<SocketReaction>();
        private bool isProcessing = false;

        public BattleManager(string Name, ITextChannel lobbyChannel)
        {
            this.Name = Name;
            this.lobbyChannel = lobbyChannel;
            Global.Client.ReactionAdded += ProcessReaction;
            Reset();
        }

        protected abstract Task ProcessReaction(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel message, SocketReaction reaction);

        public async Task ProcessTurn(bool forced)
        {
            if (isProcessing)
            {
                Console.WriteLine("Battle is still processing");
                return;
            }
            isProcessing = true;
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
            isProcessing = false;
        }

        protected abstract Task WriteBattle();

        protected abstract Task WriteBattleInit();

        protected abstract Task GameOver();

        protected abstract Task StartBattle();

        public abstract Task Reset();

        public void Dispose()
        {
            Global.Client.ReactionAdded -= ProcessReaction;
        }
    }
}