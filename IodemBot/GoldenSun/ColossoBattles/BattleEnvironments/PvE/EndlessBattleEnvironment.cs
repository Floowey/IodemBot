using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using IodemBot.Core.Leveling;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.ColossoBattles
{
    internal class EndlessBattleEnvironment : PvEEnvironment
    {
        private static readonly Dictionary<BattleDifficulty, RewardTable> chestTable = new Dictionary<BattleDifficulty, RewardTable>()
        {
            {BattleDifficulty.Tutorial, new RewardTable(){
                new DefaultReward()
            {
                    Chest = ChestQuality.Wooden,
            } } },
            {BattleDifficulty.Easy, new RewardTable()
            {
                new DefaultReward()
                {
                    Chest = ChestQuality.Wooden,
                    HasChest=true,
                    Weight = 4
                },
                new DefaultReward()
                {
                    Chest = ChestQuality.Normal,
                    HasChest=true,
                    Weight = 4
                },
                new DefaultReward()
                {
                    Chest = ChestQuality.Silver,
                    HasChest=true,
                    Weight = 1
                }
            } },
            {BattleDifficulty.Medium, new RewardTable()
            {
                new DefaultReward()
                {
                    Chest = ChestQuality.Normal,
                    HasChest=true,
                    Weight = 4
                },
                new DefaultReward()
                {
                    Chest = ChestQuality.Silver,
                    HasChest=true,
                    Weight = 4
                },
                new DefaultReward()
                {
                    Chest = ChestQuality.Gold,
                    HasChest=true,
                    Weight = 1
                }
            } },
            {BattleDifficulty.MediumRare, new RewardTable()
            {
                new DefaultReward()
                {
                    Chest = ChestQuality.Silver,
                    HasChest=true,
                    Weight = 6
                },
                new DefaultReward()
                {
                    Chest = ChestQuality.Gold,
                    HasChest=true,
                    Weight = 4
                },
                new DefaultReward()
                {
                    Chest = ChestQuality.Adept,
                    HasChest=true,
                    Weight = 1
                }
            } },
            {BattleDifficulty.Hard, new RewardTable()
            {
                new DefaultReward()
                {
                    Chest = ChestQuality.Silver,
                    HasChest=true,
                    Weight = 1
                },
                new DefaultReward()
                {
                    Chest = ChestQuality.Gold,
                    HasChest=true,
                    Weight = 6
                },
                new DefaultReward()
                {
                    Chest = ChestQuality.Adept,
                    HasChest=true,
                    Weight = 3
                }
            } }
        };

        private int LureCaps = 0;
        private int winsInARow = 0;
        private int StageLength { get; set; } = 12;
        private readonly EndlessMode mode = EndlessMode.Default;

        internal RewardTables Rewards
        {
            get
            {
                var basexp = 15 + 3 * LureCaps + winsInARow / 4;
                var DiffFactor = (int)Math.Max(2, (uint)Math.Pow((int)Difficulty + 1, 2));
                var xp = (uint)(Global.RandomNumber(basexp, 2 * basexp) * DiffFactor);
                return new RewardTables()
                {
                    new RewardTable()
                    {
                        new DefaultReward(){
                            Xp = xp,
                            Coins = xp/2,
                            Weight = 3
                        }
                    }
                };
            }
        }

        public void SetStreak(int streak)
        {
            winsInARow = streak;
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
                    return 1 + ((double)winsInARow - 3 * StageLength) / 30;
                }
            }
        }

        public EndlessBattleEnvironment(ColossoBattleService battleService, string Name, ITextChannel lobbyChannel, bool isPersistent, ITextChannel BattleChannel, EndlessMode mode = EndlessMode.Default) : base(battleService, Name, lobbyChannel, isPersistent, BattleChannel)
        {
            this.mode = mode;
            if (mode == EndlessMode.Legacy)
            {
                Factory = new PlayerFighterFactory() { DjinnOption = DjinnOption.NoDjinn, ReductionFactor = 1.5 };
            }
            _ = Reset($"init");
        }

        public override BattleDifficulty Difficulty => (BattleDifficulty)Math.Min(4, 1 + winsInARow / StageLength);

        public override void SetEnemy(string Enemy)
        {
            Battle.TeamB = new List<ColossoFighter>();
            EnemiesDatabase.GetEnemies(Difficulty, Enemy).ForEach(f => Battle.AddPlayer(f, Team.B));
            Console.WriteLine($"Up against {Battle.TeamB.First().Name}");
        }

        public override void SetNextEnemy()
        {
            Battle.TeamB.Clear();
            EnemiesDatabase.GetRandomEnemies(Difficulty, Boost).ForEach(f =>
                Battle.AddPlayer(f, Team.B)
            );

            for (int i = 0; i < LureCaps; i++)
            {
                if (Battle.SizeTeamB < 9)
                {
                    Battle.AddPlayer(EnemiesDatabase.GetRandomEnemies(Difficulty, Boost).Random(), Team.B);
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

            foreach (var player in Battle.TeamA.Concat(Battle.TeamB).OfType<PlayerFighter>())
            {
                var brokenItems = player.EquipmentWithEffect.Where(i => i.IsBroken);
                if (brokenItems.Any())
                {
                    var user = UserAccountProvider.GetById(player.Id);
                    foreach (var item in brokenItems)
                    {
                        user.Inv.GetItem(item.Name).IsBroken = item.IsBroken;
                    }
                    UserAccountProvider.StoreUser(user);
                }
            }

            if (Battle.GetWinner() == Team.A)
            {
                winsInARow++;
                var RewardTables = Rewards;
                var chests = chestTable[Difficulty];
                chests.RemoveAll(s => s is DefaultReward d && !d.HasChest);
                var lurCapBonus = new[] { 12, 10, 9, 8, 7 };
                if (!Battle.TeamB.Any(f => f.Name.Contains("Mimic")))
                {
                    chests.Add(new DefaultReward { Weight = chests.Weight * lurCapBonus[LureCaps] });
                }
                RewardTables.Add(chests);

                if(winsInARow % 10 == 0) {
                    var rt = new RewardTable
                    {
                        new DefaultReward()
                        {
                            Chest = (ChestQuality)(Math.Min(4, winsInARow / 10)),
                            HasChest = true
                        }
                    };
                    RewardTables.Add(rt);
                }

                if (Battle.TeamB.Any(f => f.Name.Contains("Djinn")))
                {
                    var djinnTable = new RewardTable();
                    var djinnWeight = (int)Difficulty;
                    if (Battle.TeamB.Any(f => f.Name.Contains("enus Djinn")))
                    {
                        djinnTable.Add(new DefaultReward() { Djinn = "Venus", Weight = 1 });
                    }
                    if (Battle.TeamB.Any(f => f.Name.Contains("ars Djinn")))
                    {
                        djinnTable.Add(new DefaultReward() { Djinn = "Mars", Weight = 1 });
                    }
                    if (Battle.TeamB.Any(f => f.Name.Contains("upiter Djinn")))
                    {
                        djinnTable.Add(new DefaultReward() { Djinn = "Jupiter", Weight = 1 });
                    }
                    if (Battle.TeamB.Any(f => f.Name.Contains("ercury Djinn")))
                    {
                        djinnTable.Add(new DefaultReward() { Djinn = "Mercury", Weight = 1 });
                    }
                    djinnTable.Add(new DefaultReward() { Weight = djinnTable.Weight * (9 - (int)Difficulty) * 2 - djinnTable.Weight });
                    RewardTables.Add(djinnTable);
                }

                winners.OfType<PlayerFighter>().ToList().ForEach(p => _ = ServerGames.UserWonBattle(UserAccountProvider.GetById(p.Id), RewardTables.GetRewards(), p.battleStats, lobbyChannel, BattleChannel));
                winners.OfType<PlayerFighter>().ToList().ForEach(p => _ = ServerGames.UserWonEndless(UserAccountProvider.GetById(p.Id), winsInARow, mode, p.battleStats.TotalTeamMates + 1, string.Join(", ", Battle.TeamA.Select(pl => pl.Name))));

                chests.RemoveAll(s => s is DefaultReward d && !d.HasChest);

                Battle.TeamA.ForEach(p =>
                {
                    p.PPrecovery += (winsInARow <= 8 * 4 && winsInARow % 4 == 0) ? 1 : 0;
                    p.RemoveNearlyAllConditions();
                    p.Buffs = new List<Buff>();
                    p.Heal((uint)(p.Stats.HP * 5 / 100));
                });

                var text = $"{winners.First().Name}'s Party wins Battle {winsInARow}! Battle will reset shortly.";
                await Task.Delay(3000);
                await StatusMessage.ModifyAsync(m => { m.Content = text; m.Embed = null; });

                await Task.Delay(3000);

                SetNextEnemy();
                Battle.turn = 0;
                _ = StartBattle();
            }
            else
            {
                var losers = winners.First().Battle.GetTeam(winners.First().enemies);
                losers.OfType<PlayerFighter>().ToList().ForEach(async p => await ServerGames.UserLostBattle(UserAccountProvider.GetById(p.Id), lobbyChannel));
                losers.OfType<PlayerFighter>().ToList().ForEach(async p => await ServerGames.UserFinishedEndless(UserAccountProvider.GetById(p.Id), winsInARow, mode));
                _ = WriteGameOver();
            }
        }

        public override async Task Reset(string msg = "")
        {
            LureCaps = 0;
            winsInARow = 0;
            await base.Reset(msg);
        }

        public override async Task AddPlayer(UserAccount user, Team team = Team.A)
        {


            if (user.Inv.GetGear(AdeptClassSeriesManager.GetClassSeries(user).Archtype).Any(i => i.Name == "Lure Cap"))
            {
                LureCaps++;
                SetNextEnemy();
            }

            await base.AddPlayer(user);
        }

        public override Task<(bool Success, string Message)> CanPlayerJoin(UserAccount user, Team team = Team.A)
        {
            var result = base.CanPlayerJoin(user, team);
            if (!result.Result.Success)
                return result;

            if (user.LevelNumber < 50 && !user.Tags.Contains("ColossoCompleted"))
            {
                return Task.FromResult((false, "Hmm.. No, I don't think I can let you on that journey yet."));
            }

            return result;
        }
        protected override EmbedBuilder GetEnemyEmbedBuilder()
        {
            var e = base.GetEnemyEmbedBuilder();
            EmbedFooterBuilder footerBuilder = new EmbedFooterBuilder();
            footerBuilder.WithText($"Battle {winsInARow + 1} - {Difficulty}");
            e.WithFooter(footerBuilder);

            return e;
        }

        protected override string GetEnemyMessageString()
        {
            return $"Welcome to Endless Battles! Show your strength and climb the leaderboards, see how far you can get. You must have reached at least level 50 to join or otherwise proven your strength in the Colosso Finals!";
        }
    }
}