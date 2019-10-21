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
        private static readonly Dictionary<BattleDifficulty, RewardTable> chestTable = new Dictionary<BattleDifficulty, RewardTable>()
        {
            {BattleDifficulty.Tutorial, new RewardTable(){
                new ChestReward()
            {
                    Chest = ChestQuality.Wooden,
            } } },
            {BattleDifficulty.Easy, new RewardTable()
            {
                new ChestReward()
                {
                    Chest = ChestQuality.Wooden,
                    Weight = 3
                },
                new ChestReward()
                {
                    Chest = ChestQuality.Normal,
                    Weight = 5
                },
                new ChestReward()
                {
                    Chest = ChestQuality.Silver,
                    Weight = 2
                }
            } },
            {BattleDifficulty.Medium, new RewardTable()
            {
                new ChestReward()
                {
                    Chest = ChestQuality.Normal,
                    Weight = 3
                },
                new ChestReward()
                {
                    Chest = ChestQuality.Silver,
                    Weight = 5
                },
                new ChestReward()
                {
                    Chest = ChestQuality.Gold,
                    Weight = 1
                }
            } },
            {BattleDifficulty.MediumRare, new RewardTable()
            {
                new ChestReward()
                {
                    Chest = ChestQuality.Silver,
                    Weight = 5
                },
                new ChestReward()
                {
                    Chest = ChestQuality.Gold,
                    Weight = 4
                },
                new ChestReward()
                {
                    Chest = ChestQuality.Adept,
                    Weight = 1
                }
            } },
            {BattleDifficulty.Hard, new RewardTable()
            {
                new ChestReward()
                {
                    Chest = ChestQuality.Silver,
                    Weight = 1
                },
                new ChestReward()
                {
                    Chest = ChestQuality.Gold,
                    Weight = 7
                },
                new ChestReward()
                {
                    Chest = ChestQuality.Adept,
                    Weight = 2
                }
            } }
        };

        private int LureCaps = 0;
        private int winsInARow = 0;
        private int StageLength { get; set; } = 12;

        internal RewardTables Rewards
        {
            get
            {
                var basexp = 20 + 5 * LureCaps + winsInARow / 4;
                var DiffFactor = (int)Math.Max(3, (uint)Math.Pow((int)Difficulty + 1, 2));
                return new RewardTables()
                {
                    new RewardTable()
                    {
                        new DefaultReward(){
                            xp = (uint)(Global.Random.Next(basexp, 2*basexp)*DiffFactor),
                            coins = (uint)(Global.Random.Next(basexp/2, basexp)*DiffFactor),
                            Weight = 3
                        },
                        new DefaultReward(){
                            xp = (uint)(Global.Random.Next(2*basexp, 3*basexp)*DiffFactor),
                        },
                        new DefaultReward(){
                            coins = (uint)(Global.Random.Next(basexp, 2*basexp)*DiffFactor),
                        },
                    }
                };
            }
        }

        private double Boost
        {
            get
            {
                if (winsInARow < 4 * StageLength)
                {
                    return 1 + ((double)winsInARow % StageLength) / 30;
                }
                else
                {
                    return 1 + ((double)winsInARow - 4 * StageLength) / 30;
                }
            }
        }

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
            EnemiesDatabase.GetRandomEnemies(Difficulty, Boost).ForEach(f =>
            {
                Battle.AddPlayer(f, ColossoBattle.Team.B);
            }
            );

            for (int i = 0; i < LureCaps; i++)
            {
                if (Battle.SizeTeamB < 9)
                {
                    Battle.AddPlayer(EnemiesDatabase.GetRandomEnemies(Difficulty, Boost).Random(), ColossoBattle.Team.B);
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
                var RewardTables = Rewards;
                var chests = chestTable[Difficulty];
                chests.RemoveAll(s => s is DefaultReward);
                if (!Battle.TeamB.Any(f => f.Name.Contains("Mimic")))
                {
                    chests.Add(new DefaultReward { Weight = chests.Weight * (14 - 2 * LureCaps) });
                }
                RewardTables.Add(chests);
                winners.OfType<PlayerFighter>().ToList().ForEach(async p => await ServerGames.UserWonBattle(p.avatar, RewardTables.GetRewards(), p.battleStats, lobbyChannel, BattleChannel, winsInARow, string.Join(", ", Battle.TeamA.Select(pl => pl.Name))));
                chests.RemoveAll(s => s is DefaultReward);

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