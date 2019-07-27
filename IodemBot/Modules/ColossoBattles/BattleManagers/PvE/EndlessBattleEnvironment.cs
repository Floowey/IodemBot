using Discord;
using Discord.WebSocket;
using IodemBot.Core.Leveling;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IodemBot.Modules.ColossoBattles
{
    internal class EndlessBattleEnvironment : PvEEnvironment
    {
        private int LureCaps = 0;
        private int winsInARow = 0;
        private int StageLength { get; set; } = 12;

        public EndlessBattleEnvironment(string Name, ITextChannel lobbyChannel, ITextChannel BattleChannel) : base(Name, lobbyChannel, BattleChannel)
        {
            _ = Reset();
        }

        public override BattleDifficulty Difficulty => (BattleDifficulty)Math.Min(4, 1 + winsInARow / StageLength);

        public override void SetEnemy(string Enemy)
        {
            Battle.TeamB = new List<ColossoFighter>();
            EnemiesDatabase.GetEnemies(Difficulty, Enemy).ForEach(f => Battle.AddPlayer(f, ColossoBattle.Team.B));
            Console.WriteLine($"Up against {Battle.TeamB.First().Name}");
        }

        public override void SetNextEnemy()
        {
            Battle.TeamB.Clear();
            EnemiesDatabase.GetRandomEnemies(Difficulty).ForEach(f =>
            {
                Battle.AddPlayer(f, ColossoBattle.Team.B);
            }
            );

            for (int i = 0; i < LureCaps; i++)
            {
                if (Battle.SizeTeamB < 9)
                {
                    Battle.AddPlayer(EnemiesDatabase.GetRandomEnemies(Difficulty, 1).Random(), ColossoBattle.Team.B);
                }
            }
            Console.WriteLine($"Up against {Battle.TeamB.First().Name}");
        }

        protected override async Task GameOver()
        {
            var winners = Battle.GetTeam(Battle.GetWinner());
            if (Battle.SizeTeamB == 0)
            {
                Console.WriteLine("Game Over with no enemies existing.");
            }
            if (Battle.GetWinner() == ColossoBattle.Team.A)
            {
                winsInARow++;
                var wasMimic = Battle.TeamB.Any(e => e.Name.Contains("Mimic"));
                winners.ConvertAll(s => (PlayerFighter)s).ForEach(async p => await ServerGames.UserWonBattle(p.avatar, winsInARow, LureCaps, p.battleStats, Difficulty, lobbyChannel, winners, wasMimic));
                Console.WriteLine("Winners rewarded.");
                Battle.TeamA.ForEach(p =>
                {
                    p.PPrecovery += (winsInARow <= 8 * 4 && winsInARow % 4 == 0) ? 1 : 0;
                    p.RemoveNearlyAllConditions();
                    p.Buffs = new List<Buff>();
                    p.Heal((uint)(p.Stats.HP * 5 / 100));
                });

                var text = $"{winners.First().Name}'s Party wins Battle {winsInARow}! Battle will reset shortly";
                await Task.Delay(2000);
                await StatusMessage.ModifyAsync(m => { m.Content = text; m.Embed = null; });

                await Task.Delay(2000);

                SetNextEnemy();
                Battle.turn = 0;
                _ = StartBattle();
            }
            else
            {
                var losers = winners.First().battle.GetTeam(winners.First().enemies);
                losers.ConvertAll(s => (PlayerFighter)s).ForEach(async p => await ServerGames.UserLostBattle(p.avatar, lobbyChannel));
                _ = WriteGameOver();
            }
        }

        public override async Task Reset()
        {
            LureCaps = 0;
            winsInARow = 0;
            await base.Reset();
        }

        protected override async Task AddPlayer(SocketReaction reaction)
        {
            if (PlayerMessages.Values.Any(s => (s.avatar.ID == reaction.UserId)))
            {
                return;
            }
            SocketGuildUser player = (SocketGuildUser)reaction.User.Value;
            var playerAvatar = UserAccounts.GetAccount(player);
            var factory = new PlayerFighterFactory();
            var p = factory.CreatePlayerFighter(player);

            if (playerAvatar.Inv.GetGear(AdeptClassSeriesManager.GetClassSeries(playerAvatar).Archtype).Any(i => i.Name == "Lure Cap"))
            {
                LureCaps++;
                SetNextEnemy();
            }

            await AddPlayer(p);
        }

        protected override EmbedBuilder GetEnemyEmbedBuilder()
        {
            var e = base.GetEnemyEmbedBuilder();
            EmbedFooterBuilder footerBuilder = new EmbedFooterBuilder();
            footerBuilder.WithText($"Battle {winsInARow + 1} - {Difficulty}");
            e.WithFooter(footerBuilder);

            return e;
        }
    }
}