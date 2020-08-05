﻿using System;
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
    internal class SingleBattleEnvironment : PvEEnvironment
    {
        private int LureCaps = 0;

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

        public SingleBattleEnvironment(string Name, ITextChannel lobbyChannel, bool isPersistent, ITextChannel BattleChannel, BattleDifficulty diff) : base(Name, lobbyChannel, isPersistent, BattleChannel)
        {
            internalDiff = diff;
            _ = Reset();
        }

        public override BattleDifficulty Difficulty => internalDiff;
        internal BattleDifficulty internalDiff = BattleDifficulty.Easy;

        internal RewardTables Rewards
        {
            get
            {
                var basexp = 12 + 5 * LureCaps;
                var DiffFactor = (int)Math.Max(3, (uint)Math.Pow((int)Difficulty + 1, 2));
                var xp = (uint)(Global.Random.Next(basexp, 2 * basexp) * DiffFactor);
                return new RewardTables()
                {
                    new RewardTable()
                    {
                        new DefaultReward(){
                            xp = xp,
                            coins = xp/2,
                            Weight = 3
                        },
                        new DefaultReward(){
                            xp = xp*2,
                            coins = xp/4
                        },
                        new DefaultReward(){
                            xp = xp/2,
                            coins = xp
                        },
                    }
                };
            }
        }

        public override void SetEnemy(string Enemy)
        {
            Battle.TeamB = new List<ColossoFighter>();
            EnemiesDatabase.GetEnemies(Difficulty, Enemy).ForEach(f => Battle.AddPlayer(f, Team.B));
            Console.WriteLine($"Up against {Battle.TeamB.First().Name}");
        }

        protected override async Task AddPlayer(SocketReaction reaction)
        {
            if (PlayerMessages.Values.Any(s => (s.avatar.ID == reaction.UserId)))
            {
                return;
            }
            SocketGuildUser player = (SocketGuildUser)reaction.User.Value;
            var playerAvatar = UserAccounts.GetAccount(player);
            var p = Factory.CreatePlayerFighter(player);

            if (Battle.SizeTeamA == 0 && Difficulty == BattleDifficulty.Easy && playerAvatar.LevelNumber < 10)
            {
                internalDiff = BattleDifficulty.Tutorial;
                SetNextEnemy();
            } else if (Difficulty == BattleDifficulty.Tutorial && playerAvatar.LevelNumber >= 10)
            {
                internalDiff = BattleDifficulty.Easy;
                SetNextEnemy();
            }

            if (Difficulty == BattleDifficulty.Easy && playerAvatar.LevelNumber < 10) return;
            if (Difficulty == BattleDifficulty.Medium && playerAvatar.LevelNumber < 30) return;
            if (Difficulty == BattleDifficulty.Hard && playerAvatar.LevelNumber < 50) return;

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
                Battle.AddPlayer(f, Team.B);
            }
            );

            for (int i = 0; i < LureCaps; i++)
            {
                if (Battle.SizeTeamB < 9)
                {
                    Battle.AddPlayer(EnemiesDatabase.GetRandomEnemies(Difficulty, 1).Random(), Team.B);
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
                var RewardTables = Rewards;
                // Get the appropiate chest rewards table
                var chests = chestTable[Difficulty];
                chests.RemoveAll(s => s is DefaultReward d && !d.HasChest);

                // If there was *no* mimic, add a counter weight
                var lurCapBonus = new[] { 16, 12, 10, 9, 8 };
                if (!Battle.TeamB.Any(f => f.Name.Contains("Mimic")))
                {
                    chests.Add(new DefaultReward() { Weight = chests.Weight * lurCapBonus[LureCaps] });
                }
                RewardTables.Add(chests);

                if (Battle.TeamB.Any(f => f.Name.Contains("Djinn")))
                {
                    var djinnTable = new RewardTable();
                    var djinnWeight = (int)Difficulty;
                    if(Battle.TeamB.Any(f => f.Name.Contains("Venus Djinn")))
                    {
                        djinnTable.Add(new DefaultReward() { Djinn = "Venus", Weight = 1 });
                    }
                    if (Battle.TeamB.Any(f => f.Name.Contains("Mars Djinn")))
                    {
                        djinnTable.Add(new DefaultReward() { Djinn = "Mars", Weight = 1 });
                    }
                    if (Battle.TeamB.Any(f => f.Name.Contains("Jupiter Djinn")))
                    {
                        djinnTable.Add(new DefaultReward() { Djinn = "Jupiter", Weight = 1 });
                    }
                    if (Battle.TeamB.Any(f => f.Name.Contains("Mercury Djinn")))
                    {
                        djinnTable.Add(new DefaultReward() { Djinn = "Mercury", Weight = 1 });
                    }
                    djinnTable.Add(new DefaultReward() { Weight = djinnTable.Weight*(10-(int)Difficulty)*3-djinnTable.Weight });
                    RewardTables.Add(djinnTable);
                }
                
                winners.OfType<PlayerFighter>().ToList().ForEach(async p => await ServerGames.UserWonBattle(p.avatar, RewardTables.GetRewards(), p.battleStats, lobbyChannel, BattleChannel));
                winners.OfType<PlayerFighter>().ToList().ForEach(async p => await ServerGames.UserWonSingleBattle(p.avatar, lobbyChannel, Difficulty));

                chests.RemoveAll(s => s is DefaultReward d && !d.HasChest);
                _ = WriteGameOver();
            }
            else
            {
                var losers = winners.First().battle.GetTeam(winners.First().enemies);
                losers.ForEach(p => p.Moves.OfType<Djinn>().ToList().ForEach(d => { d.Summon(p); d.CoolDown = 0; }));
                losers.ConvertAll(s => (PlayerFighter)s).ForEach(async p => await ServerGames.UserLostBattle(p.avatar, lobbyChannel));
                _ = WriteGameOver();
            }

            await Task.CompletedTask;
        }

        public override async Task Reset()
        {
            LureCaps = 0;
            await base.Reset();

            _ = EnemyMessage.AddReactionsAsync(new IEmote[]
            {
                    Emote.Parse("<:Bronze:537214232203100190>"),
                    Emote.Parse("<:Silver:537214282891395072>"),
                    Emote.Parse("<:Gold:537214319591555073>")
            });
            wasJustReset = true;
        }

        private static readonly Dictionary<BattleDifficulty, string> medals = new Dictionary<BattleDifficulty, string>(){
            {BattleDifficulty.Tutorial , ""},
            {BattleDifficulty.Easy, "<:Bronze:537214232203100190>" },
            {BattleDifficulty.Medium, "<:Silver:537214282891395072>" },
            {BattleDifficulty.Hard, "<:Gold:537214319591555073>" }
        };

        private static readonly Dictionary<BattleDifficulty, int> limits = new Dictionary<BattleDifficulty, int>()
        {
            {BattleDifficulty.Tutorial , 0},
            {BattleDifficulty.Easy, 10},
            {BattleDifficulty.Medium, 30},
            {BattleDifficulty.Hard, 50 }
        };

        protected override string GetEnemyMessageString()
        {
            return $"Welcome to {Name} Battle! The difficulty is set to {medals[Difficulty]} ***{Difficulty}*** {medals[Difficulty]}!\n\nReact with <:Fight:536919792813211648> to join the {Name} Battle and press <:Battle:536954571256365096> when you are ready to battle! You must have reached level {limits[Difficulty]} to enter.";
        }
    }
}