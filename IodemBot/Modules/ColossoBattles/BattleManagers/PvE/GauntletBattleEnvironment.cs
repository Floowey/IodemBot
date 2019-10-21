using Discord;
using Discord.WebSocket;
using IodemBot.Core.Leveling;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static IodemBot.Modules.ColossoBattles.EnemiesDatabase;

namespace IodemBot.Modules.ColossoBattles
{
    internal class GauntletBattleEnvironment : PvEEnvironment
    {
        public Dungeon Dungeon;
        public DungeonMatchup matchup;
        public List<DungeonMatchup>.Enumerator enumerator;
        private bool EndOfDungeon = false;
        public bool HasPlayer { get { return Battle.SizeTeamA > 0; } }
        public DateTime LastEnemySet = DateTime.MinValue;
        public bool IsReady { get { return !IsActive && !HasPlayer && DateTime.Now.Subtract(LastEnemySet).Seconds > 20; } }

        public GauntletBattleEnvironment(string Name, ITextChannel lobbyChannel, ITextChannel BattleChannel, string DungeonName) : base(Name, lobbyChannel, BattleChannel)
        {
            SetEnemy(DungeonName);
        }

        public override BattleDifficulty Difficulty => throw new NotImplementedException();

        public override void SetEnemy(string Enemy)
        {
            Dungeon = GetDungeon(Enemy);
            enumerator = Dungeon.Matchups.GetEnumerator();
            LastEnemySet = DateTime.Now;
            _ = Reset();
        }

        public override void SetNextEnemy()
        {
            if (enumerator.MoveNext())
            {
                matchup = enumerator.Current;
                Battle.TeamB.Clear();
                matchup.Enemy.ForEach(e => Battle.AddPlayer((NPCEnemy)e.Clone(), ColossoBattle.Team.B));
                EndOfDungeon = false;
            }
            else
            {
                EndOfDungeon = true;
            }
        }

        protected override EmbedBuilder GetEnemyEmbedBuilder()
        {
            var builder = base.GetEnemyEmbedBuilder();
            if (matchup.Image != null)
            {
                builder.WithThumbnailUrl(matchup.Image);
            }
            return builder;
        }

        public override async Task Reset()
        {
            enumerator = Dungeon.Matchups.GetEnumerator();
            matchup = enumerator.Current;
            await base.Reset();
            var e = new EmbedBuilder();
            e.WithThumbnailUrl(Dungeon.Image);
            e.WithDescription(EnemyMessage.Content);
            await EnemyMessage.ModifyAsync(m =>
            {
                m.Content = "";
                m.Embed = e.Build();
            });
        }

        protected override async Task AddPlayer(SocketReaction reaction)
        {
            if (PlayerMessages.Values.Any(s => (s.avatar.ID == reaction.UserId)))
            {
                return;
            }
            SocketGuildUser player = (SocketGuildUser)reaction.User.Value;
            var playerAvatar = UserAccounts.GetAccount(player);

            if (!Dungeon.Requirement.Applies(playerAvatar))
            {
                return;
            }

            await base.AddPlayer(reaction);
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
                    winners.ConvertAll(s => (PlayerFighter)s).ForEach(async p => await ServerGames.UserWonBattle(p.avatar, matchup.RewardTables.GetRewards(), p.battleStats, lobbyChannel, BattleChannel));
                }

                Battle.TeamA.ForEach(p =>
                    {
                        p.RemoveNearlyAllConditions();
                        p.Buffs = new List<Buff>();
                        p.Heal((uint)(p.Stats.HP * 5 / 100));
                    });

                SetNextEnemy();

                if (!EndOfDungeon)
                {
                    var text = $"{winners.First().Name}'s Party wins Battle! \n{matchup.FlavourText}";
                    await Task.Delay(2000);
                    await StatusMessage.ModifyAsync(m => { m.Content = text; m.Embed = null; });
                    await Task.Delay(2000);
                    Battle.turn = 0;
                    _ = StartBattle();
                }
                else
                {
                    _ = WriteGameOver();
                }
            }
            else
            {
                var losers = winners.First().battle.GetTeam(winners.First().enemies);

                losers.ConvertAll(s => (PlayerFighter)s).ForEach(async p => await ServerGames.UserLostBattle(p.avatar, lobbyChannel));

                _ = WriteGameOver();
            }
        }

        protected override async Task WriteGameOver()
        {
            await Task.Delay(3000);
            var winners = Battle.GetTeam(Battle.GetWinner());
            var text = GetWinMessageString();
            await StatusMessage.ModifyAsync(m => { m.Content = text; m.Embed = null; });
            await Task.Delay(2000);
            SetEnemy(DefaultDungeons.Random().Name);
        }
    }
}