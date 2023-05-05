using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using IodemBot.Core.Leveling;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;
using static IodemBot.ColossoBattles.EnemiesDatabase;

namespace IodemBot.ColossoBattles
{
    internal class GauntletBattleEnvironment : PvEEnvironment
    {
        public Dungeon Dungeon;
        public int DungeonNr;
        private bool _endOfDungeon;
        public List<DungeonMatchup>.Enumerator Enumerator;
        public DateTime LastEnemySet = DateTime.MinValue;
        public DungeonMatchup Matchup;

        public GauntletBattleEnvironment(ColossoBattleService battleService, string name, ITextChannel lobbyChannel,
            ITextChannel battleChannel, string dungeonName, bool isPersistent) : base(battleService, name, lobbyChannel,
            isPersistent, battleChannel)
        {
            SetEnemy(dungeonName);
        }

        public bool HasPlayer => Battle.SizeTeamA > 0;
        public bool IsReady => !IsActive && !HasPlayer && DateTime.Now.Subtract(LastEnemySet).TotalSeconds >= 20;

        public override void SetEnemy(string enemy)
        {
            Dungeon = GetDungeon(enemy);
            PlayersToStart = Dungeon.MaxPlayer;
            Enumerator = Dungeon.Matchups.GetEnumerator();
            LastEnemySet = DateTime.Now;
            _ = Reset($"Enemy set : {enemy}");
        }

        public override void SetNextEnemy()
        {
            Matchup = Dungeon.Matchups.ElementAt(DungeonNr);
            Battle.TeamB.Clear();
            var curEnemies = Matchup.Enemy;
            if (Matchup.Shuffle) curEnemies.Shuffle();

            var allTags = Battle.TeamA.OfType<PlayerFighter>()
                   .SelectMany(p => UserAccountProvider.GetById(p.Id).Tags);
            curEnemies = curEnemies.Where(e =>
            {
                var checkTags = e.Tags.Where(t => t.Contains("HasTag"));

                foreach (var tag in checkTags)
                {
                    var splits = tag.Split(':');
                    var keyWord = splits.First();
                    var Negative = keyWord.StartsWith('!');
                    keyWord = Negative ? keyWord[1..] : keyWord;
                    keyWord = keyWord == "HasTag" ? "HasTagAny" : keyWord;
                    var checkAny = keyWord.Contains("Any");

                    var normalTags = splits.Skip(1).Where(t => !t.Contains('*'));
                    var wildCards = splits.Skip(1).Where(t => t.Contains('*')).Select(s => s[0..^1]);

                    if (checkAny) //Check Any
                    {
                        var valid = (!normalTags.Any() || normalTags.Any(allTags.Contains))
                        && (!wildCards.Any() || wildCards.Any(t => allTags.Any(a => a.StartsWith(t))));
                        if (valid == Negative)
                            return false;
                    }
                    else //Check All
                    {
                        var valid = (!normalTags.Any() || normalTags.All(allTags.Contains))
                        && (!wildCards.Any() || wildCards.All(t => allTags.Any(a => a.StartsWith(t))));
                        if (valid == Negative)
                            return false;
                    }
                }
                return true;
            }).ToList();

            curEnemies.Reverse();
            curEnemies.ForEach(e =>
            {
                if (curEnemies.Count(p => p.Name.Equals(e.Name) || p.Name[..^2].Equals(e.Name)) > 1)
                    e.Name = $"{e.Name} {curEnemies.Count(p => p.Name.Equals(e.Name))}";
            });
            curEnemies.Reverse();
            curEnemies.ForEach(e => Battle.AddPlayer(e, Team.B));

            if (Matchup.HealBefore)
                Battle.TeamA.ForEach(f =>
                {
                    f.RemoveAllConditions();
                    f.Heal(1000);
                    f.RestorePp(1000);
                });

            if (Matchup.Keywords.Contains("Cure"))
                Battle.TeamA.ForEach(f => { f.RemoveAllConditions(); });

            Matchup.Keywords
                .Where(s => s.StartsWith("Status"))
                .Select(s => s[6..])
                .Select(s => Enum.Parse<Condition>(s, true)).ToList()
                .ForEach(c => Battle.TeamA.ForEach(p => p.AddCondition(c)));
        }

        protected override EmbedBuilder GetEnemyEmbedBuilder()
        {
            var builder = base.GetEnemyEmbedBuilder();
            if (!Matchup.Image.IsNullOrEmpty()) builder.WithThumbnailUrl(Matchup.Image);
            return builder;
        }

        public override async Task Reset(string msg = "")
        {
            Enumerator = Dungeon.Matchups.GetEnumerator();
            Matchup = Enumerator.Current;
            await base.Reset(msg);
            if (!IsPersistent)
                return;
            var e = new EmbedBuilder();
            e.WithThumbnailUrl(Dungeon.Image);
            e.WithDescription(EnemyMessage.Content);
            await EnemyMessage.ModifyAsync(m =>
            {
                m.Content = "";
                m.Embed = e.Build();
            });
        }

        public override Task<(bool Success, string Message)> CanPlayerJoin(UserAccount user, Team team = Team.A)
        {
            var result = base.CanPlayerJoin(user, team);
            if (!result.Result.Success)
                return result;

            if (!Dungeon.Requirement.Applies(user))
                return Task.FromResult((false, "This journey would be too dangerous for you!"));

            return result;
        }

        protected override string GetEnemyMessageString()
        {
            return $"Welcome to {Dungeon.Name}!\n{Dungeon?.FlavourText}";
        }

        protected override string GetStartBattleString()
        {
            var msg = string.Join(", ", PlayerMessages.Select(v => $"<@{v.Value.Id}>"));
            SummonsMessage.ModifyAsync(m => m.Content = Matchup.FlavourText);
            return $"{msg} get into Position!\n{string.Join("\n", Battle.Log)}";
        }

        protected override async Task GameOver()
        {
            var winners = Battle.GetTeam(Battle.GetWinner());
            if (Battle.GetWinner() == Team.A)
            {
                if (Battle.GetWinner() == Team.A)
                    winners.OfType<PlayerFighter>().ToList().ForEach(p =>
                    {
                        var r = (RewardTables)Matchup.RewardTables.Clone();
                        r.ForEach(t => t.ForEach(reward => reward.RequireTag.RemoveAll(s => s.Equals($"@{Battle.OutValue}"))));
                        _ = ServerGames.UserWonBattle(UserAccountProvider.GetById(p.Id),
                            r.GetRewards(), p.BattleStats, LobbyChannel, BattleChannel);
                    });

                Battle.TeamA.ForEach(p =>
                {
                    p.RemoveNearlyAllConditions();
                    p.Buffs = new List<Buff>();
                    //p.Heal((uint)(p.Stats.HP * 5 / 100));
                });

                DungeonNr++;

                var setMatchup = Matchup.Keywords.FirstOrDefault(m => m.StartsWith("ToMatchup"));
                if (setMatchup is not null)
                    DungeonNr = setMatchup.Split('@').Skip(1).Select(int.Parse).Random();

                var allTags = Battle.TeamA.OfType<PlayerFighter>()
                    .SelectMany(p => UserAccountProvider.GetById(p.Id).Tags);
                var checkTags = Matchup.Keywords.Where(t => t.Contains("HasTag"));
                foreach (var tag in checkTags)
                {
                    var args = tag.Split('@');
                    var BattleNumbers = args.Skip(1).Select(int.Parse);
                    var splits = args.First().Split(':');
                    var keyWord = splits.First();
                    var Negative = keyWord.StartsWith('!');
                    keyWord = Negative ? keyWord[1..] : keyWord;
                    keyWord = keyWord == "HasTag" ? "HasTagAny" : keyWord;
                    var checkAny = keyWord.Contains("Any");

                    var normalTags = splits.Skip(1).Where(t => !t.Contains('*'));
                    var wildCards = splits.Skip(1).Where(t => t.Contains('*')).Select(s => s[0..^1]);

                    if (checkAny) //Check Any
                    {
                        var valid = (!normalTags.Any() || normalTags.Any(allTags.Contains))
                        && (!wildCards.Any() || wildCards.Any(t => allTags.Any(a => a.StartsWith(t))));
                        if (valid == Negative)
                            continue;
                    }
                    else //Check All
                    {
                        var valid = (!normalTags.Any() || normalTags.All(allTags.Contains))
                        && (!wildCards.Any() || wildCards.All(t => allTags.Any(a => a.StartsWith(t))));
                        if (valid == Negative)
                            continue;
                    }
                    DungeonNr = BattleNumbers.Random();
                }
                if (Battle.OutValue >= 0)
                    DungeonNr = Battle.OutValue;
                if (DungeonNr >= 100)
                {
                    var taggedDungeon = Dungeon.Matchups.First(d => d.Keywords.Contains($"@{DungeonNr}"));
                    DungeonNr = Dungeon.Matchups.IndexOf(taggedDungeon);
                }

                // Set To Next Dungeon if applicable
                if (DungeonNr >= Dungeon.Matchups.Count || Matchup.EnemyNames.Any(e => e.Contains("End")))
                    _endOfDungeon = true;
                else
                    SetNextEnemy();

                if (!_endOfDungeon)
                {
                    await SummonsMessage.ModifyAsync(m => m.Content = Matchup.FlavourText);
                    var text = $"{winners.First().Name}'s party wins the Battle!";
                    await Task.Delay(2000);
                    await StatusMessage.ModifyAsync(m =>
                    {
                        m.Content = text;
                        m.Embed = null;
                    });
                    await Task.Delay(2000);
                    Battle.TurnNumber = 0;
                    _ = StartBattle();
                }
                else
                {
                    winners.OfType<PlayerFighter>().ToList().ForEach(p =>
                        _ = ServerGames.UserWonDungeon(UserAccountProvider.GetById(p.Id), Dungeon, LobbyChannel));

                    if (EventSchedule.CheckEvent("Halloween") && Global.RandomNumber(0, 5) == 0)
                    {
                        var r = new List<Rewardable> { new DefaultReward { Dungeon = "Halloween Special" } };
                        winners.OfType<PlayerFighter>().ToList().ForEach(p =>
                            _ = ServerGames.UserWonBattle(UserAccountProvider.GetById(p.Id), r, new BattleStats(),
                                LobbyChannel, BattleChannel));
                    }

                    if (EventSchedule.CheckEvent("Christmas") && Global.RandomNumber(0, 5) == 0)
                    {
                        var r = new List<Rewardable> { new DefaultReward { Dungeon = "A Very Golden Christmas" } };
                        winners.OfType<PlayerFighter>().ToList().ForEach(p =>
                            _ = ServerGames.UserWonBattle(UserAccountProvider.GetById(p.Id), r, new BattleStats(),
                                LobbyChannel, BattleChannel));
                    }

                    _ = WriteGameOver();
                }
            }
            else
            {
                var losers = winners.First().Battle.GetTeam(winners.First().enemies);

                losers.ConvertAll(s => (PlayerFighter)s).ForEach(p =>
                   _ = ServerGames.UserLostBattle(UserAccountProvider.GetById(p.Id), LobbyChannel));

                _ = WriteGameOver();
            }
        }

        protected override async Task WriteGameOver()
        {
            await Task.Delay(3000);
            var text = GetWinMessageString();
            await StatusMessage.ModifyAsync(m =>
            {
                m.Content = text;
                m.Embed = null;
            });
            await Task.Delay(2000);
            SetEnemy(DefaultDungeons.Random().Name);
        }
    }
}