using System;
using System.Collections.Generic;
using System.Linq;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.ColossoBattles
{
    public class ColossoBattle
    {
        public List<ColossoFighter> TeamA = new();
        public List<ColossoFighter> TeamB = new();
        public bool IsActive { get; set; }
        public bool TurnActive { get; set; }
        public int TurnNumber { get; set; }

        public int SizeTeamA => TeamA.Count;

        public int SizeTeamB => TeamB.Count;

        public List<string> Log { get; set; } = new();
        public int OutValue { get; set; } = -1;

        public List<ColossoFighter> GetTeam(Team team)
        {
            return team == Team.A ? TeamA : TeamB;
        }

        public void Start()
        {
            TurnNumber = 0;
            Log.Clear();
            if (TeamB.Any(p => p.Tags.Contains("MirrorAll")))
            {
                var mirrorCount = TeamB.Count(p => p.Tags.Contains("MirrorAll"));
                for (int i = 0; i < mirrorCount; i++)
                {
                    var otherTags = TeamB.Where(p => p.Tags.Contains("MirrorAll")).ElementAt(i).Tags.Except(new[] { "Mirror", "MirrorAll" });
                    var nickname = TeamB.Where(p => p.Tags.Contains("MirrorAll")).ElementAt(i).Name;
                    foreach (var player in TeamA)
                    {
                        var enemy = EnemiesDatabase.GetEnemy("Next");
                        enemy.ReplaceWith(player);
                        enemy.Heal(1000);
                        enemy.RemoveNearlyAllConditions();
                        enemy.Tags.AddRange(otherTags);
                        enemy.Tags.RemoveAll(t => t.StartsWith("Mirror"));
                        enemy.Name = nickname == "Mirror" ? enemy.Name : nickname;
                        //enemy = (NpcEnemy)enemy.Clone();
                        AddPlayer(enemy, Team.B);
                    }
                }
                TeamB.RemoveAll(p => p.Tags.Contains("MirrorAll"));
            }
            if (TeamB.Any(p => p.Tags.Contains("Mirror")))
            {
                foreach (var mirror in TeamB.OfType<NpcEnemy>().Where(p => p.Tags.Contains("Mirror")))
                {
                    var mirrorName = mirror.Name;
                    var otherTags = mirror.Tags.Except(new[] { "Mirror", "MirrorAll" });
                    mirror.ReplaceWith(TeamA.OfType<PlayerFighter>().OrderByDescending(p => p.Stats.MaxHP + p.Stats.MaxPP + p.Stats.Atk + p.Stats.Def + p.Stats.Spd).First());
                    mirror.Heal(1000);
                    mirror.RemoveNearlyAllConditions();
                    mirror.Name = mirrorName == "Mirror" ? mirror.Name : mirrorName;
                    mirror.Tags.AddRange(otherTags);
                    mirror.Tags.RemoveAll(t => t.StartsWith("Mirror"));
                    //AddPlayer(mirror, Team.B);
                }
                TeamB.RemoveAll(p => p.Tags.Contains("MirrorAll"));
            }

            foreach (var fighter in TeamA.Concat(TeamB).Where(f => f.Tags.Any(t => t.StartsWith("Boost:"))))
            {
                foreach (var tag in fighter.Tags.Where(t => t.StartsWith("Boost")))
                {
                    var splits = tag.Split(':');
                    var stat = splits.Length == 3 ? splits.ElementAt(2) : "All";
                    var amount = float.Parse(splits.ElementAt(1));

                    switch (stat)
                    {
                        case "HP":
                        case "MaxHP":
                            fighter.Stats.MaxHP = (int)(fighter.Stats.MaxHP * amount / 100);
                            fighter.Stats.HP = fighter.Stats.MaxHP;
                            break;

                        case "MaxPP":
                        case "PP":
                            fighter.Stats.MaxPP = (int)(fighter.Stats.MaxPP * amount / 100);
                            fighter.Stats.PP = fighter.Stats.MaxPP; break;
                        case "Atk":
                            fighter.Stats.Atk = (int)(fighter.Stats.Atk * amount / 100);
                            break;

                        case "Def":
                            fighter.Stats.Def = (int)(fighter.Stats.Def * amount / 100);
                            break;

                        case "Spd":
                            fighter.Stats.Spd = (int)(fighter.Stats.Spd * amount / 100);
                            break;

                        case "All":
                        default:
                            fighter.Stats *= amount;
                            fighter.Stats *= 0.01;
                            break;
                    }
                }
            }

            foreach (var fighter in TeamA.Concat(TeamB).OfType<NpcEnemy>().Where(f => f.Tags.Any(t => t.StartsWith("ExtraTurns:"))))
            {
                var splits = fighter.Tags.First(t => t.StartsWith("ExtraTurns")).Split(':');
                var turns = int.Parse(splits.ElementAt(1));
                fighter.ExtraTurns = turns;
            }

            IsActive = true;
            foreach (var p in TeamA.Concat(TeamB))
            {
                switch (p.Passive.Name)
                {
                    case "Stone Skin":
                        if (p.IsAlive)
                        {
                            p.DefensiveMult = p.Passive.args[p.PassiveLevel];
                            Log.Add($"{p.Name}'s skin hardens.");
                        }
                        break;

                    case "Instant Ignition":
                        if (p.IsAlive)
                        {
                            p.OffensiveMult = p.Passive.args[p.PassiveLevel];
                            Log.Add($"{p.Name} gets fired up.");
                        }
                        break;

                    case "Soothing Song":
                        if (p.IsAlive && Global.RandomNumber(0, 100) < p.Passive.args[p.PassiveLevel])
                        {
                            var c = p.RemoveCondition(new[] { Condition.Poison, Condition.Venom, Condition.Haunt });
                            if (c > 0)
                                Log.Add($"{p.Name} is relieved of any ailments");
                        }
                        break;

                    case "Vital Spark":
                        if (!p.IsAlive)
                        {
                            Log.Add($"{p.Name}'s inner spark reignites.");
                            p.Revive((uint)p.Passive.args[p.PassiveLevel]);
                        }
                        break;

                    case "Fiery Reflex":
                        if (p.IsAlive && Global.RandomNumber(0, 100) < p.Passive.args[p.PassiveLevel])
                        {
                            p.AddCondition(Condition.Counter);
                            Log.Add($"{p.Name} strikes a battle pose.");
                        }
                        break;

                    case "Brisk Flow":
                        if (p.IsAlive)
                        {
                            p.RestorePp((uint)(p.Stats.MaxPP * p.Passive.args[p.PassiveLevel]));
                            Log.Add($"A brisk flow refills {p.Name}'s PP.");
                        }
                        break;

                    case "Petrichor Scent":
                        if (p.IsAlive)
                        {
                            p.Heal((uint)(p.Stats.MaxHP * p.Passive.args[p.PassiveLevel]));
                            Log.Add($"The smell of geosmin lifts {p.Name}'s health.");
                        }
                        break;
                }

                if (p is NpcEnemy) p.SelectRandom();

                if (p is PlayerFighter fighter)
                {
                    fighter.BattleStats = new BattleStats();
                    fighter.BattleStats.TotalTeamMates += TeamA.Count - 1;
                    if (TeamA.Count == 1) fighter.BattleStats.SoloBattles++;
                }
            }
        }

        public bool ForceTurn()
        {
            if (TurnActive) return false;

            Console.WriteLine("Forcing turn.");
            var fighters = new List<ColossoFighter>(TeamA);
            fighters.AddRange(TeamB);
            fighters.ForEach(f =>
            {
                if (!f.HasSelected)
                {
                    f.SelectRandom();
                    if (f is PlayerFighter player) player.AutoTurnsInARow++;
                }
            });
            return Turn();
        }

        public void AddPlayer(ColossoFighter player, Team team)
        {
            if (IsActive) return;

            GetTeam(team).Add(player);
            player.party = team;
            player.Battle = this;
        }

        public bool Turn()
        {
            if (!IsActive) return false;

            if (TurnActive) return false;

            if (!(TeamA.All(p => p.HasSelected) && TeamB.All(p => p.HasSelected))) return false;

            if (TurnNumber != 0)
                Log.Clear();
            OutValue = -1;
            if (SizeTeamB == 0 || SizeTeamA == 0)
            {
                Console.WriteLine("The stupid bug happened");
                Log.Add("Error occured. You win.");
                IsActive = false;
                return true;
            }

            TurnActive = true;
            Log.Add($"Turn {++TurnNumber}");

            try
            {
                StartTurn(); // moves with priority
                MainTurn();
                ExtraTurn(); // extra Turns
            }
            catch (Exception e)
            {
                Log.Add(e.ToString());
                Console.WriteLine("Turn Processing Error: " + e);
            }
            finally
            {
                EndTurn();
            }

            //Check for Game Over
            if (GameOver()) IsActive = false;
            TurnActive = false;
            return true;
        }

        private List<ColossoFighter> GetFighterOrder()
        {
            var fighters = TeamA.Concat(TeamB).ToList();
            fighters.Shuffle();

            fighters.Where(p => p.IsAlive && p.Passive.Equals("Tail Wind")).ToList().ForEach(p => Log.Add($"{p.Name} swiftly acts."));
            return fighters
                .OrderByDescending(f => f.Passive.Equals("Tail Wind") && TurnNumber == 0 &&
                    (f.PassiveLevel == 2 || f.SelectedMove is StatusPsynergy || (f.SelectedMove is OffensivePsynergy && f.PassiveLevel == 1)))
                .ThenBy(f => f.Tags.Contains("OathIdleness"))
                .ThenByDescending(f => f.Stats.Spd * f.MultiplyBuffs("Speed"))
                .ToList();
        }

        private void StartTurn()
        {
            GetFighterOrder().ForEach(f => Log.AddRange(f.StartTurn()));
        }

        private void MainTurn()
        {
            GetFighterOrder().ForEach(f => Log.AddRange(f.MainTurn()));
        }

        private void ExtraTurn()
        {
            GetFighterOrder().ForEach(f => Log.AddRange(f.ExtraTurn()));
        }

        private void EndTurn()
        {
            GetFighterOrder().ForEach(f => Log.AddRange(f.EndTurn()));
            TeamB.RemoveAll(m => m.Name == "Runner");
        }

        private bool GameOver()
        {
            return !(TeamA.Any(p => p.IsAlive) && TeamB.Any(p => p.IsAlive));
        }

        public Team GetWinner()
        {
            return TeamA.Any(p => p.IsAlive) ? Team.A : Team.B;
        }
    }
}