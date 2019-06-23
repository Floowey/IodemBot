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
    internal class SingleBattleManager : PvEBattleManager
    {
        private int LureCaps = 0;

        public SingleBattleManager(string Name, ITextChannel lobbyChannel, ITextChannel BattleChannel, BattleDifficulty diff) : base(Name, lobbyChannel, BattleChannel)
        {
            internalDiff = diff;
        }

        public override BattleDifficulty Difficulty => internalDiff;
        private BattleDifficulty internalDiff = BattleDifficulty.Easy;

        public override void SetEnemy(string Enemy)
        {
            Battle.TeamB = new List<ColossoFighter>();
            EnemiesDatabase.GetEnemies(Difficulty, Enemy).ForEach(f => Battle.AddPlayer(f, ColossoBattle.Team.B));
            Console.WriteLine($"Up against {Battle.TeamB.First().name}");
        }

        protected override async Task AddPlayer(SocketReaction reaction)
        {
            if (PlayerMessages.Values.Any(s => (s.avatar.ID == reaction.UserId)))
            {
                return;
            }
            SocketGuildUser player = (SocketGuildUser)reaction.User.Value;
            var playerAvatar = UserAccounts.GetAccount(player);
            var p = new PlayerFighter(player);

            if (Name == "Bronze")
            {
                if (playerAvatar.LevelNumber < 10 && PlayerMessages.Count == 0)
                {
                    internalDiff = BattleDifficulty.Tutorial;
                    SetNextEnemy();
                }
                else
                {
                    if (Difficulty != BattleDifficulty.Easy)
                    {
                        internalDiff = BattleDifficulty.Easy;
                        SetNextEnemy();
                    }
                }
            }
            if (playerAvatar.Inv.GetGear(AdeptClassSeriesManager.GetClassSeries(playerAvatar).Archtype).Any(i => i.Name == "Lure Cap"))
            {
                LureCaps++;
                SetNextEnemy();
            }

            await AddPlayer(p);
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
            Console.WriteLine($"Up against {Battle.TeamB.First().name}");
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
                var wasMimic = Battle.TeamB.Any(e => e.name.Contains("Mimic"));
                winners.ConvertAll(s => (PlayerFighter)s).ForEach(async p => await ServerGames.UserWonBattle(p.avatar, 1, LureCaps, p.battleStats, Difficulty, lobbyChannel, winners, wasMimic));

                _ = WriteGameOver();
            }
            else
            {
                var losers = winners.First().battle.GetTeam(winners.First().enemies);
                losers.ConvertAll(s => (PlayerFighter)s).ForEach(async p => await ServerGames.UserLostBattle(p.avatar, lobbyChannel));
                _ = WriteGameOver();
            }
            await Task.CompletedTask;
        }
    }
}