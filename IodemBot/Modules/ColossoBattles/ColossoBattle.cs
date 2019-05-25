using IodemBot.Core.UserManagement;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Modules.ColossoBattles
{
    public class ColossoBattle
    {
        public enum Team { A, B }

        //public static NPCEnemy enemy;
        public List<ColossoFighter> TeamA = new List<ColossoFighter>();

        public List<ColossoFighter> TeamB = new List<ColossoFighter>();
        public bool isActive = false;
        public uint sizeTeamA = 0;
        public uint sizeTeamB = 0;
        public int turn = 0;
        public List<string> log = new List<string>();
        public bool turnActive = false;

        public ColossoBattle()
        {
        }

        public void Start()
        {
            isActive = true;
            turn = 0;
            log.Clear();
            TeamA.ForEach(p =>
            {
                if (p is NPCEnemy)
                {
                    p.SelectRandom();
                }

                if (p is PlayerFighter)
                {
                    ((PlayerFighter)p).battleStats = new BattleStats();
                    ((PlayerFighter)p).battleStats.TotalTeamMates += TeamA.Count - 1;
                    if (TeamA.Count == 1)
                    {
                        ((PlayerFighter)p).battleStats.SoloBattles++;
                    }
                }
            });

            TeamB.ForEach(p =>
            {
                if (p is NPCEnemy)
                {
                    p.SelectRandom();
                }

                if (p is PlayerFighter)
                {
                    ((PlayerFighter)p).battleStats = new BattleStats();
                    ((PlayerFighter)p).battleStats.TotalTeamMates += TeamB.Count - 1;
                    if (TeamB.Count == 1)
                    {
                        ((PlayerFighter)p).battleStats.SoloBattles++;
                    }
                }
            });

            UserAccounts.SaveAccounts();
        }

        public bool ForceTurn()
        {
            if (turnActive)
            {
                return false;
            }

            Console.WriteLine("Forcing turn.");
            List<ColossoFighter> fighters = new List<ColossoFighter>(TeamA);
            fighters.AddRange(TeamB);
            fighters.ForEach(f =>
            {
                if (!f.hasSelected)
                {
                    f.SelectRandom();
                    if (f is PlayerFighter)
                    {
                        ((PlayerFighter)f).AutoTurnPool--;
                        ((PlayerFighter)f).AutoTurnsInARow++;
                    }
                }
            });
            return Turn();
        }

        public bool Turn()
        {
            if (!isActive)
            {
                return false;
            }

            if (turnActive)
            {
                return false;
            }

            log.Clear();
            if (!(TeamA.All(p => p.hasSelected) && TeamB.All(p => p.hasSelected)))
            {
                return false;
            }
            turnActive = true;
            bool b = true;
            log.Add($"Turn {++turn}");
            //Stop Timer, just in Case
            Console.WriteLine("Starting to process Turn");

            //Start Turn for things like Defend
            log.AddRange(StartTurn());
            Console.WriteLine("Finished StartTurn()");

            //Main Turn
            log.AddRange(MainTurn());
            Console.WriteLine("Finished MainTurn()");

            //Main Turn
            log.AddRange(ExtraTurn());
            Console.WriteLine("Finished ExtraTurn()");

            //End Turn
            log.AddRange(EndTurn());
            Console.WriteLine("Finished EndTurn()");

            //Check for Game Over
            if (GameOver())
            {
                isActive = false;
            }
            turnActive = false;

            Console.WriteLine("Done processing Turn");

            return b;
        }

        public void AddPlayer(ColossoFighter player, Team team)
        {
            if (isActive)
            {
                return;
            }

            if (TeamA.Any(p => p.imgUrl != "" && p.imgUrl == player.imgUrl))
            {
                return;
            }

            if (team == Team.A)
            {
                TeamA.Add(player);
                player.party = Team.A;
                player.enemies = Team.B;
                sizeTeamA++;
            }
            else
            {
                TeamB.Add(player);
                player.party = Team.B;
                player.enemies = Team.A;
                sizeTeamB++;
            }

            player.battle = this;
        }

        public List<ColossoFighter> GetTeam(Team team)
        {
            if (team == Team.A)
            {
                return TeamA;
            }
            else
            {
                return TeamB;
            }
        }

        private List<string> StartTurn()
        {
            List<string> turnLog = new List<string>();
            List<ColossoFighter> fighters = new List<ColossoFighter>(TeamA);
            fighters.AddRange(TeamB);
            fighters = fighters.OrderByDescending(f => f.stats.Spd * f.MultiplyBuffs("Speed")).ToList();
            fighters.ForEach(f => { turnLog.AddRange(f.StartTurn()); });
            return turnLog;
        }

        private List<string> MainTurn()
        {
            List<string> turnLog = new List<string>();
            List<ColossoFighter> fighters = new List<ColossoFighter>(TeamA);
            fighters.AddRange(TeamB);
            fighters = fighters.OrderByDescending(f => f.stats.Spd * f.MultiplyBuffs("Speed")).ToList();
            fighters.ForEach(f => { turnLog.AddRange(f.MainTurn()); });
            return turnLog;
        }

        private List<string> ExtraTurn()
        {
            List<string> turnLog = new List<string>();
            List<ColossoFighter> fighters = new List<ColossoFighter>(TeamA);
            fighters.AddRange(TeamB);
            fighters = fighters.OrderByDescending(f => f.stats.Spd * f.MultiplyBuffs("Speed")).ToList();
            fighters.ForEach(f => { turnLog.AddRange(f.ExtraTurn()); });
            return turnLog;
        }

        private List<string> EndTurn()
        {
            List<string> turnLog = new List<string>();
            List<ColossoFighter> fighters = new List<ColossoFighter>(TeamA);
            fighters.AddRange(TeamB);
            fighters = fighters.OrderByDescending(f => f.stats.Spd * f.MultiplyBuffs("Speed")).ToList();
            fighters.ForEach(f => { turnLog.AddRange(f.EndTurn()); });
            return turnLog;
        }

        public void ResetGame()
        {
            TeamA = new List<ColossoFighter>();
            TeamB = new List<ColossoFighter>();
            log.Clear();
            turn = 0;
            isActive = false;
        }

        private bool GameOver()
        {
            return !TeamA.Where(p => p.IsAlive()).Any() || !TeamB.Where(p => p.IsAlive()).Any();
        }

        public Team GetWinner()
        {
            if (TeamA.Where(p => p.IsAlive()).Any())
            {
                return Team.A;
            }
            else
            {
                return Team.B;
            }
        }
    }
}