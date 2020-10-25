using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using IodemBot.Core.Leveling;
using IodemBot.Core.UserManagement;
using IodemBot.Discord;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;
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
        public bool IsReady { get { return !IsActive && !HasPlayer && DateTime.Now.Subtract(LastEnemySet).TotalSeconds >= 20; } }

        public GauntletBattleEnvironment(string Name, ITextChannel lobbyChannel, ITextChannel BattleChannel, string DungeonName, bool isPersistent) : base(Name, lobbyChannel, isPersistent, BattleChannel)
        {
            SetEnemy(DungeonName);
        }

        public override BattleDifficulty Difficulty => throw new NotImplementedException();

        public override void SetEnemy(string Enemy)
        {
            Dungeon = GetDungeon(Enemy);
            PlayersToStart = Dungeon.MaxPlayer;
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
                var curEnemies = matchup.Enemy;
                if (matchup.Shuffle)
                {
                    curEnemies.Shuffle();
                }
                curEnemies.Reverse();
                curEnemies.ForEach(e =>
                {
                    if (curEnemies.Count(p => p.Name.Equals(e.Name) || p.Name[0..^2].Equals(e.Name)) > 1)
                    {
                        e.Name = $"{e.Name} {curEnemies.Count(p => p.Name.Equals(e.Name))}";
                    }
                });
                curEnemies.Reverse();
                curEnemies.ForEach(e => Battle.AddPlayer(e, Team.B));

                if (matchup.HealBefore)
                {
                    Battle.TeamA.ForEach(f =>
                    {
                        f.RemoveAllConditions();
                        f.Heal(1000);
                        f.RestorePP(1000);
                    });
                }

                if (matchup.Keywords.Contains("Cure"))
                {
                    Battle.TeamA.ForEach(f =>
                    {
                        f.RemoveAllConditions();
                    });
                }

                matchup.Keywords
                    .Where(s => s.StartsWith("Status"))
                    .Select(s => s.Substring(6))
                    .Select(s => Enum.Parse<Condition>(s, true)).ToList()
                    .ForEach(c => Battle.TeamA.ForEach(p => p.AddCondition(c)));

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
            if (!matchup.Image.IsNullOrEmpty())
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
            if (PlayerMessages.Values.Any(s => (s.Id == reaction.UserId)))
            {
                return;
            }
            SocketGuildUser player = (SocketGuildUser)reaction.User.Value;
            var playerAvatar = EntityConverter.ConvertUser(player);

            if (!Dungeon.Requirement.Applies(playerAvatar))
            {
                return;
            }

            await base.AddPlayer(reaction);
        }

        protected override string GetEnemyMessageString()
        {
            return $"Welcome to {Dungeon.Name}!\n{Dungeon?.FlavourText}";
        }

        protected override string GetStartBattleString()
        {
            string msg = string.Join(", ", PlayerMessages.Select(v => $"<@{v.Value.Id}>"));
            SummonsMessage.ModifyAsync(m => m.Content = matchup.FlavourText);
            return $"{msg} get into Position!";
        }

        protected override async Task GameOver()
        {
            var winners = Battle.GetTeam(Battle.GetWinner());
            if (Battle.GetWinner() == Team.A)
            {
                if (Battle.GetWinner() == Team.A)
                {
                    winners.OfType<PlayerFighter>().ToList().ForEach(async p => await ServerGames.UserWonBattle(UserAccountProvider.GetById(p.Id), matchup.RewardTables.GetRewards(), p.battleStats, lobbyChannel, BattleChannel));
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
                    await SummonsMessage.ModifyAsync(m => m.Content = matchup.FlavourText);
                    var text = $"{winners.First().Name}'s Party wins Battle!";
                    await Task.Delay(2000);
                    await StatusMessage.ModifyAsync(m => { m.Content = text; m.Embed = null; });
                    await Task.Delay(2000);
                    Battle.turn = 0;
                    _ = StartBattle();
                }
                else
                {
                    winners.OfType<PlayerFighter>().ToList().ForEach(async p => await ServerGames.UserWonDungeon(UserAccountProvider.GetById(p.Id), Dungeon, lobbyChannel));

                    if (DateTime.Now <= new DateTime(2020, 11, 8) && Global.Random.Next(4) == 0)
                    {
                        var r = new List<Rewardable>() { new DefaultReward() { Dungeon = "Halloween Special" } };
                        winners.OfType<PlayerFighter>().ToList().ForEach(async p => await ServerGames.UserWonBattle(UserAccountProvider.GetById(p.Id), r, new BattleStats(), lobbyChannel, BattleChannel));
                    }

                    _ = WriteGameOver();
                }
            }
            else
            {
                var losers = winners.First().battle.GetTeam(winners.First().enemies);

                
                losers.ConvertAll(s => (PlayerFighter)s).ForEach(async p => await ServerGames.UserLostBattle(UserAccountProvider.GetById(p.Id), lobbyChannel));

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