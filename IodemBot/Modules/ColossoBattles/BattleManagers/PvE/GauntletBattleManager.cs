using Discord;
using IodemBot.Core.Leveling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static IodemBot.Modules.ColossoBattles.EnemiesDatabase;

namespace IodemBot.Modules.ColossoBattles
{
    internal class GauntletBattleManager : PvEBattleManager
    {
        public Dungeon Dungeon;
        public DungeonMatchup matchup;
        public List<DungeonMatchup>.Enumerator enumerator;
        private bool EndOfDungeon = false;

        public GauntletBattleManager(string Name, ITextChannel lobbyChannel, ITextChannel BattleChannel, string DungeonName) : base(Name, lobbyChannel, BattleChannel)
        {
            SetEnemy(DungeonName);
            Console.WriteLine("hi");
            _ = Reset();
        }

        public override BattleDifficulty Difficulty => throw new NotImplementedException();

        public override void SetEnemy(string Enemy)
        {
            Dungeon = GetDungeon(Enemy);
            enumerator = Dungeon.Matchups.GetEnumerator();
        }

        public override void SetNextEnemy()
        {
            if (enumerator.MoveNext())
            {
                matchup = enumerator.Current;
                Battle.TeamB.Clear();
                matchup.Enemy.ForEach(e => Battle.AddPlayer(e, ColossoBattle.Team.B));
                EndOfDungeon = false;
            }
            else
            {
                EndOfDungeon = true;
            }
        }

        public override async Task Reset()
        {
            enumerator = Dungeon.Matchups.GetEnumerator();
            matchup = enumerator.Current;
            await base.Reset();
        }

        protected override string GetEnemyMessageString()
        {
            return $"{base.GetEnemyMessageString()}\n{Dungeon?.FlavourText ?? "Poop"}";
        }

        protected override string GetStartBattleString()
        {
            string msg = PlayerMessages
                        .Aggregate("", (s, v) => s += $"<@{v.Value.avatar.ID}>, ");
            return $"{matchup.FlavourText}\n{msg} get into Position!";
        }

        protected override async Task GameOver()
        {
            var winners = Battle.GetTeam(Battle.GetWinner());
            if (Battle.GetWinner() == ColossoBattle.Team.A)
            {
                if (Battle.GetWinner() == ColossoBattle.Team.A)
                {
                    winners.ConvertAll(s => (PlayerFighter)s).ForEach(async p => await ServerGames.UserWonBattle(p.avatar, matchup, p.battleStats, lobbyChannel));
                }

                Battle.TeamA.ForEach(p =>
                    {
                        p.RemoveNearlyAllConditions();
                        p.Buffs = new List<Buff>();
                        p.Heal((uint)(p.stats.HP * 5 / 100));
                    });

                SetNextEnemy();

                if (!EndOfDungeon)
                {
                    var text = $"{winners.First().name}'s Party wins Battle! \n {matchup.FlavourText}.";
                    await Task.Delay(2000);
                    await StatusMessage.ModifyAsync(m => { m.Content = text; m.Embed = null; });
                    await Task.Delay(2000);
                    Battle.turn = 0;
                    _ = StartBattle();
                }
                else
                { _ = WriteGameOver(); }
            }
            else
            {
                var losers = winners.First().battle.GetTeam(winners.First().enemies);

                losers.ConvertAll(s => (PlayerFighter)s).ForEach(async p => await ServerGames.UserLostBattle(p.avatar, lobbyChannel));

                _ = WriteGameOver();
            }
        }
    }
}