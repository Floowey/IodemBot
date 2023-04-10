using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using IodemBot.Core.Leveling;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.ColossoBattles
{
    internal class SingleBattleEnvironment : PvEEnvironment
    {
        private static readonly Dictionary<BattleDifficulty, RewardTable> ChestTable = new()
        {
            {
                BattleDifficulty.Tutorial,
                new RewardTable
                {
                    new DefaultReward
                    {
                        Chest = ChestQuality.Wooden
                    }
                }
            },
            {
                BattleDifficulty.Easy,
                new RewardTable
                {
                    new DefaultReward
                    {
                        Chest = ChestQuality.Wooden,
                        HasChest = true,
                        Weight = 4
                    },
                    new DefaultReward
                    {
                        Chest = ChestQuality.Normal,
                        HasChest = true,
                        Weight = 4
                    },
                    new DefaultReward
                    {
                        Chest = ChestQuality.Silver,
                        HasChest = true,
                        Weight = 1
                    }
                }
            },
            {
                BattleDifficulty.Medium,
                new RewardTable
                {
                    new DefaultReward
                    {
                        Chest = ChestQuality.Normal,
                        HasChest = true,
                        Weight = 4
                    },
                    new DefaultReward
                    {
                        Chest = ChestQuality.Silver,
                        HasChest = true,
                        Weight = 4
                    },
                    new DefaultReward
                    {
                        Chest = ChestQuality.Gold,
                        HasChest = true,
                        Weight = 1
                    }
                }
            },
            {
                BattleDifficulty.MediumRare,
                new RewardTable
                {
                    new DefaultReward
                    {
                        Chest = ChestQuality.Silver,
                        HasChest = true,
                        Weight = 6
                    },
                    new DefaultReward
                    {
                        Chest = ChestQuality.Gold,
                        HasChest = true,
                        Weight = 4
                    },
                    new DefaultReward
                    {
                        Chest = ChestQuality.Adept,
                        HasChest = true,
                        Weight = 1
                    }
                }
            },
            {
                BattleDifficulty.Hard,
                new RewardTable
                {
                    new DefaultReward
                    {
                        Chest = ChestQuality.Silver,
                        HasChest = true,
                        Weight = 1
                    },
                    new DefaultReward
                    {
                        Chest = ChestQuality.Gold,
                        HasChest = true,
                        Weight = 7
                    },
                    new DefaultReward
                    {
                        Chest = ChestQuality.Adept,
                        HasChest = true,
                        Weight = 2
                    }
                }
            }
        };

        private static readonly Dictionary<BattleDifficulty, string> Medals = new()
        {
            { BattleDifficulty.Tutorial, "" },
            { BattleDifficulty.Easy, "<:Bronze:537214232203100190>" },
            { BattleDifficulty.Medium, "<:Silver:537214282891395072>" },
            { BattleDifficulty.Hard, "<:Gold:537214319591555073>" }
        };

        private static readonly Dictionary<BattleDifficulty, int> Limits = new()
        {
            { BattleDifficulty.Tutorial, 0 },
            { BattleDifficulty.Easy, 10 },
            { BattleDifficulty.Medium, 30 },
            { BattleDifficulty.Hard, 50 }
        };

        private int _lureCaps;

        internal BattleDifficulty InternalDiff = BattleDifficulty.Easy;

        public SingleBattleEnvironment(ColossoBattleService battleService, string name, ITextChannel lobbyChannel,
            bool isPersistent, ITextChannel battleChannel, BattleDifficulty diff) : base(battleService, name,
            lobbyChannel, isPersistent, battleChannel)
        {
            InternalDiff = diff;
            _ = Reset("init");
        }

        public virtual BattleDifficulty Difficulty => InternalDiff;

        internal RewardTables Rewards
        {
            get
            {
                var basexp = 16 + 3 * _lureCaps;
                var diffFactor = (int)Math.Max(3, (uint)Math.Pow((int)Difficulty + 1, 2));
                var xp = (uint)(Global.RandomNumber(basexp, 2 * basexp) * diffFactor);
                return new RewardTables
                {
                    new()
                    {
                        new DefaultReward
                        {
                            Xp = xp,
                            Coins = xp / 2,
                            Weight = 3
                        }
                    }
                };
            }
        }

        public override void SetEnemy(string enemy)
        {
            Battle.TeamB = new List<ColossoFighter>();
            EnemiesDatabase.GetEnemies(Difficulty, enemy).ForEach(f => Battle.AddPlayer(f, Team.B));
            // Console.WriteLine($"Up against {Battle.TeamB.First().Name}");
        }

        public override async Task AddPlayer(UserAccount user, Team team = Team.A)
        {
            if (Battle.SizeTeamA == 0 && Difficulty == BattleDifficulty.Easy && user.LevelNumber < 10)
            {
                InternalDiff = BattleDifficulty.Tutorial;
                SetNextEnemy();
            }
            else if (Difficulty == BattleDifficulty.Tutorial && user.LevelNumber >= 10)
            {
                InternalDiff = BattleDifficulty.Easy;
                SetNextEnemy();
            }

            if (user.Inv.GetGear(user.ClassSeries.Archtype).Any(i => i.Name == "Lure Cap"))
            {
                _lureCaps++;
                SetNextEnemy();
            }

            await base.AddPlayer(user);
        }

        public override Task<(bool Success, string Message)> CanPlayerJoin(UserAccount user, Team team = Team.A)
        {
            var result = base.CanPlayerJoin(user, team);
            if (!result.Result.Success)
                return result;

            if (Difficulty > BattleDifficulty.Easy && user.LevelNumber < Limits[Difficulty])
                return Task.FromResult((false,
                    $"You need to be at least {Limits[Difficulty]} to join a {Difficulty} battle."));

            return result;
        }

        public override void SetNextEnemy()
        {
            Battle.TeamB.Clear();
            EnemiesDatabase.GetRandomEnemies(Difficulty).ForEach(f =>
                Battle.AddPlayer(f, Team.B)
            );

            for (var i = 0; i < _lureCaps; i++)
                if (Battle.SizeTeamB < 9)
                    Battle.AddPlayer(EnemiesDatabase.GetRandomEnemies(Difficulty).Random(), Team.B);
            //Console.WriteLine($"Up against {Battle.TeamB.First().Name}");
        }

        protected override async Task GameOver()
        {
            var winners = Battle.GetTeam(Battle.GetWinner());
            if (Battle.SizeTeamB == 0) Console.WriteLine("Game Over with no enemies existing.");
            if (Battle.GetWinner() == Team.A)
            {
                var rewardTables = Rewards;
                // Get the appropiate chest rewards table
                var chests = ChestTable[Difficulty];
                chests.RemoveAll(s => s is DefaultReward { HasChest: false });

                // If there was *no* mimic, add a counter weight
                //var lurCapBonus = new[] { 16, 12, 10, 9, 8 };
                var lurCapBonus = new[] { 12, 10, 9, 8, 7 };

                if (!Battle.TeamB.Any(f => f.Name.Contains("Mimic")))
                    chests.Add(new DefaultReward { Weight = chests.Weight * lurCapBonus[_lureCaps] });
                rewardTables.Add(chests);

                if (Battle.TeamB.Any(f => f.Name.Contains("Djinn")))
                {
                    var djinnTable = new RewardTable();
                    if (Battle.TeamB.Any(f => f.Name.Contains("enus Djinn")))
                        djinnTable.Add(new DefaultReward { Djinn = "Venus", Weight = 1 });
                    if (Battle.TeamB.Any(f => f.Name.Contains("ars Djinn")))
                        djinnTable.Add(new DefaultReward { Djinn = "Mars", Weight = 1 });
                    if (Battle.TeamB.Any(f => f.Name.Contains("upiter Djinn")))
                        djinnTable.Add(new DefaultReward { Djinn = "Jupiter", Weight = 1 });
                    if (Battle.TeamB.Any(f => f.Name.Contains("ercury Djinn")))
                        djinnTable.Add(new DefaultReward { Djinn = "Mercury", Weight = 1 });
                    djinnTable.Add(new DefaultReward
                    { Weight = djinnTable.Weight * 3 * (8 - (int)Difficulty) });
                    rewardTables.Add(djinnTable);
                }

                winners.OfType<PlayerFighter>().ToList().ForEach(p =>
                    _ = ServerGames.UserWonBattle(UserAccountProvider.GetById(p.Id), rewardTables.GetRewards(),
                        p.BattleStats, LobbyChannel, BattleChannel));
                winners.OfType<PlayerFighter>().ToList().ForEach(p =>
                    _ = ServerGames.UserWonSingleBattle(UserAccountProvider.GetById(p.Id), Difficulty));

                chests.RemoveAll(s => s is DefaultReward { HasChest: false });
                _ = WriteGameOver();
            }
            else
            {
                var losers = winners.First().Battle.GetTeam(winners.First().enemies);
                losers.ForEach(p => p.Moves.OfType<Djinn>().ToList().ForEach(d =>
                {
                    d.Summon(p);
                    d.CoolDown = 0;
                }));
                losers.ConvertAll(s => (PlayerFighter)s).ForEach(p =>
                   _ = ServerGames.UserLostBattle(UserAccountProvider.GetById(p.Id), LobbyChannel));
                _ = WriteGameOver();
            }

            await Task.CompletedTask;
        }

        public override async Task Reset(string msg = "")
        {
            _lureCaps = 0;
            await base.Reset(msg);

            _ = EnemyMessage.AddReactionsAsync(new IEmote[]
            {
                Emote.Parse("<:Bronze:537214232203100190>"),
                Emote.Parse("<:Silver:537214282891395072>"),
                Emote.Parse("<:Gold:537214319591555073>")
            });
        }

        protected override string GetEnemyMessageString()
        {
            return
                $"Welcome to {Name} Battle! The difficulty is set to {Medals[Difficulty]} ***{Difficulty}*** {Medals[Difficulty]}!\n\nReact with <:Fight:536919792813211648> to join the {Name} Battle and press <:Battle:536954571256365096> when you are ready to battle! You must have reached level {Limits[Difficulty]} to enter.";
        }
    }
}