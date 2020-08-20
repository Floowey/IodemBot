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

namespace IodemBot.Modules.ColossoBattles
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
                    Weight = 2
                },
                new DefaultReward()
                {
                    Chest = ChestQuality.Gold,
                    HasChest=true,
                    Weight = 7
                },
                new DefaultReward()
                {
                    Chest = ChestQuality.Adept,
                    HasChest=true,
                    Weight = 2
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
                var basexp = 12 + 3 * LureCaps + winsInARow / 4;
                var DiffFactor = (int)Math.Max(2, (uint)Math.Pow((int)Difficulty + 1, 2));
                var xp = (uint)(Global.RandomNumber(basexp, 2 * basexp) * DiffFactor);
                return new RewardTables()
                {
                    new RewardTable()
                    {
                        new DefaultReward(){
                            xp = xp,
                            coins = xp/2,
                            Weight = 3
                        }
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
                    return 1 + ((double)winsInARow - 3 * StageLength) / 30;
                }
            }
        }

        public EndlessBattleEnvironment(string Name, ITextChannel lobbyChannel, bool isPersistent, ITextChannel BattleChannel, EndlessMode mode = EndlessMode.Default) : base(Name, lobbyChannel, isPersistent, BattleChannel)
        {
            this.mode = mode;
            if(mode == EndlessMode.Legacy)
            {
                Factory = new PlayerFighterFactory() { DjinnOption = DjinnOption.NoDjinn, ReductionFactor = 1.5 };
            }
            _ = Reset();
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
            if (Battle.GetWinner() == Team.A)
            {
                winsInARow++;
                var RewardTables = Rewards;
                var chests = chestTable[Difficulty];
                chests.RemoveAll(s => s is DefaultReward d && !d.HasChest);
                var lurCapBonus = new[] { 16, 12, 10, 9, 8 };
                if (!Battle.TeamB.Any(f => f.Name.Contains("Mimic")))
                {
                    chests.Add(new DefaultReward { Weight = chests.Weight * lurCapBonus[LureCaps] });
                }
                RewardTables.Add(chests);

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
                    djinnTable.Add(new DefaultReward() { Weight = djinnTable.Weight * (10 - (int)Difficulty) * 2 - djinnTable.Weight } );
                    RewardTables.Add(djinnTable);
                }

                winners.OfType<PlayerFighter>().ToList().ForEach(async p => await ServerGames.UserWonBattle(p.avatar, RewardTables.GetRewards(), p.battleStats, lobbyChannel, BattleChannel));
                winners.OfType<PlayerFighter>().ToList().ForEach(async p => await ServerGames.UserWonEndless(p.avatar, lobbyChannel, winsInARow, mode, p.battleStats.TotalTeamMates+1, string.Join(", ", Battle.TeamA.Select(pl => pl.Name))));

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
                var losers = winners.First().battle.GetTeam(winners.First().enemies);
                losers.OfType<PlayerFighter>().ToList().ForEach(async p => await ServerGames.UserLostBattle(p.avatar, lobbyChannel));
                losers.OfType<PlayerFighter>().ToList().ForEach(async p => await ServerGames.UserFinishedEndless(p.avatar, lobbyChannel, winsInARow, mode));
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

            if (playerAvatar.LevelNumber < 50 && !playerAvatar.Tags.Contains("ColossoCompleted")) return;
            
            var p = Factory.CreatePlayerFighter(player);

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

        protected override string GetEnemyMessageString()
        {
            return $"Welcome to Endless Battles! Show your strength and climb the leaderboards, see how far you can get. You must have reached at least level 50 to join or otherwise proven your strength in the Colosso Finals!";
        }
    }
}