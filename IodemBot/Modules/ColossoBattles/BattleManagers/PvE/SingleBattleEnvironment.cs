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
    internal class SingleBattleEnvironment : PvEEnvironment
    {
        private int LureCaps = 0;

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

        public SingleBattleEnvironment(string Name, ITextChannel lobbyChannel, ITextChannel BattleChannel, BattleDifficulty diff) : base(Name, lobbyChannel, BattleChannel)
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
                var basexp = 20 + 5 * LureCaps;
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
                            coins = xp/10
                        },
                        new DefaultReward(){
                            coins = xp,
                            xp = xp/10
                        },
                    }
                };
            }
        }

        public override void SetEnemy(string Enemy)
        {
            Battle.TeamB = new List<ColossoFighter>();
            EnemiesDatabase.GetEnemies(Difficulty, Enemy).ForEach(f => Battle.AddPlayer(f, ColossoBattle.Team.B));
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
            var factory = new PlayerFighterFactory();
            var p = factory.CreatePlayerFighter(player);

            if (Difficulty == BattleDifficulty.Tutorial || Difficulty == BattleDifficulty.Easy)
            {
                if (playerAvatar.LevelNumber < 10 && Battle.SizeTeamA == 0)
                {
                    internalDiff = BattleDifficulty.Tutorial;
                    SetNextEnemy();
                }
                else if (Difficulty == BattleDifficulty.Tutorial)
                {
                    internalDiff = BattleDifficulty.Easy;
                    SetNextEnemy();
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
                var RewardTables = Rewards;
                // Get the appropiate chest rewards table
                var chests = chestTable[Difficulty];
                chests.RemoveAll(s => s is DefaultReward);

                // If there was *no* mimic, add a counter weight
                if (!Battle.TeamB.Any(f => f.Name.Contains("Mimic")))
                {
                    chests.Add(new DefaultReward { Weight = chests.Weight * (14 - 2 * LureCaps - Battle.SizeTeamA) });
                }
                RewardTables.Add(chests);
                winners.OfType<PlayerFighter>().ToList().ForEach(async p => await ServerGames.UserWonBattle(p.avatar, RewardTables.GetRewards(), p.battleStats, lobbyChannel, BattleChannel));
                chests.RemoveAll(s => s is DefaultReward);
                _ = WriteGameOver();
            }
            else
            {
                var losers = winners.First().battle.GetTeam(winners.First().enemies);
                losers.ConvertAll(s => (PlayerFighter)s).ForEach(async p => await ServerGames.UserLostBattle(p.avatar, lobbyChannel));
                _ = WriteGameOver();
            }
            LureCaps = 0;
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

        protected override string GetEnemyMessageString()

        {
            return $"Welcome to {Name} Battle! The difficulty is set to {medals[Difficulty]} ***{Difficulty}*** {medals[Difficulty]}!\n\nReact with <:Fight:536919792813211648> to join the {Name} Battle and press <:Battle:536954571256365096> when you are ready to battle!";
        }
    }
}