using IodemBot.Core.UserManagement;
using IodemBot.Modules.GoldenSunMechanics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Modules.ColossoBattles
{
    public class ColossoBattle
    {
        public enum Team { A,B}
        //public static NPCEnemy enemy;
        public List<ColossoFighter> TeamA = new List<ColossoFighter>();
        public List<ColossoFighter> TeamB = new List<ColossoFighter>();
        public bool isActive = false;
        public uint sizeTeamA = 0;
        public uint sizeTeamB = 0;
        public List<string> log = new List<string>();
        public bool turnActive = false;

        public ColossoBattle()
        {
            
        }

        public void Start()
        {
            isActive = true;
            TeamA.ForEach(p =>
            {
                if (p is NPCEnemy) p.selectRandom();
                if (p is PlayerFighter)
                {
                    ((PlayerFighter)p).avatar.totalTeamMates += TeamA.Count - 1;
                    if (TeamA.Count == 1) ((PlayerFighter)p).avatar.soloBattles++;
                }
            });

            TeamB.ForEach(p =>
            {
                if (p is NPCEnemy) p.selectRandom();
                if (p is PlayerFighter)
                {
                    ((PlayerFighter)p).avatar.totalTeamMates += TeamB.Count - 1;
                    if (TeamB.Count == 1) ((PlayerFighter)p).avatar.soloBattles++;
                }
            });

            UserAccounts.SaveAccounts();
        }

        public void TimeUp()
        {
            // Select random Move from players movepools

            // Force Next Turn
            Turn();
        }

        public bool ForceTurn()
        {
            if (turnActive) return false;
            Console.WriteLine("Forcing turn.");
            List<ColossoFighter> fighters = new List<ColossoFighter>(TeamA);
            fighters.AddRange(TeamB);
            fighters.ForEach(f =>
            { if (!f.hasSelected)
                    f.selectRandom();
            });
            return Turn();
        }

        public bool Turn()
        {
            if (!isActive) return false;
            if (turnActive) return false;
            log.Clear();
            if(TeamA.Aggregate(false, (p, s) => !s.hasSelected ? true : p) ||
                TeamB.Aggregate(false, (p, s) => !s.hasSelected ? true : p))
            {
                return false;
            }
            turnActive = true;
            bool b = true;
            //Stop Timer, just in Case
            Console.WriteLine("Starting to process Turn");

            //Start Turn for things like Defend
            StartTurn();

            //Main Turn
            log.AddRange(MainTurn());

            //End Turn
            EndTurn();

            //Check for Game Over
            if (gameOver()){
                isActive = false;
            }
            turnActive = false;
            return b;
        }

        public void AddPlayer(ColossoFighter player, Team team)
        {
            if (isActive) return;
            if (TeamA.Any(p => p.imgUrl != "" && p.imgUrl == player.imgUrl)) return;
            if (team == Team.A){
                TeamA.Add(player);
                player.party = Team.A;
                player.enemies = Team.B;
                sizeTeamA++;
            } else
            {
                TeamB.Add(player);
                player.party = Team.B;
                player.enemies = Team.A;
                sizeTeamB++;
            }
            player.Revive(100);
            player.RemoveAllConditions();
            player.stats.HP = player.stats.maxHP;
            player.stats.PP = player.stats.maxPP;

            player.battle = this;
        }

        public List<ColossoFighter> getTeam(Team team)
        {
            if (team == Team.A)
            {
                return TeamA;
            } else
            {
                return TeamB;
            }
        }

        private void StartTurn()
        {
            List<ColossoFighter> fighters = new List<ColossoFighter>(TeamA);
            fighters.AddRange(TeamB);
            fighters.OrderBy(f => f.stats.Spd);
            fighters.ForEach(f => f.StartTurn());
        }

        private List<string> MainTurn()
        {
            List<string> turnLog = new List<string>();
            List<ColossoFighter> fighters = new List<ColossoFighter>(TeamA);
            fighters.AddRange(TeamB);
            //fighters.Sort();
            fighters = fighters.OrderByDescending(f => f.stats.Spd).ToList();
            fighters.ForEach(f => { turnLog.AddRange(f.MainTurn());});
            return turnLog;
        }

        private void EndTurn()
        {
            List<ColossoFighter> fighters = new List<ColossoFighter>(TeamA);
            fighters.AddRange(TeamB);
            fighters.OrderBy(f => f.stats.Spd);
            fighters.ForEach(f => f.EndTurn());
        }

        public void resetGame()
        {
            TeamA = new List<ColossoFighter>();
            TeamB = new List<ColossoFighter>();
            isActive = false;
        }

        private bool gameOver()
        {
            return !TeamA.Where(p => p.IsAlive()).Any() || !TeamB.Where(p => p.IsAlive()).Any();
        }

        public Team getWinner()
        {
            if (TeamA.Where(p => p.IsAlive()).Any()) return Team.A; else return Team.B;
        }
    }
}
